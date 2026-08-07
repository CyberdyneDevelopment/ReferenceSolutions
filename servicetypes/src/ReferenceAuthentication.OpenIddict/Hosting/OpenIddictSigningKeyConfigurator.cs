using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using ReferenceAuthentication.OpenIddict.Logging;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.TokenManagers;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Hosting;

/// <summary>
/// Configures <see cref="OpenIddictServerOptions"/> with the explicit issuer and the RS256 signing +
/// encryption credentials, resolved on demand from the gateway-backed configuration providers and the
/// secret manager — the same providers every other FDW service injects. Runs once, when OpenIddict first
/// resolves its server options.
/// </summary>
/// <remarks>
/// OpenIddict's <see cref="IConfigureOptions{TOptions}"/> seam is synchronous, so — exactly like
/// <c>MsSqlConnectionFactory.ResolvePasswordSync</c> — this opens a short-lived scope to resolve the
/// scoped providers and blocks once on the async read at options-build time. No hosted service, no
/// mutable singleton. Config + key resolution are inlined here (no shared helper) — this configurator
/// injects only the providers IT needs: the header/typed-body config providers and the secret-manager
/// provider.
/// </remarks>
internal sealed class OpenIddictSigningKeyConfigurator : IConfigureOptions<OpenIddictServerOptions>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OpenIddictSigningKeyConfigurator> _logger;

    public OpenIddictSigningKeyConfigurator(
        IServiceScopeFactory scopeFactory,
        ILoggerFactory? loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        _scopeFactory = scopeFactory;
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<OpenIddictSigningKeyConfigurator>();
    }

    /// <inheritdoc />
    // Why: sync-over-async (.GetAwaiter().GetResult()) because IConfigureOptions.Configure is a
    // synchronous framework seam. Resolved once at options-build via a short-lived scope, mirroring
    // MsSqlConnectionFactory.ResolvePasswordSync — the sanctioned FDW idiom for a sync/singleton context
    // that needs a scoped secret-backed provider. Runs in ASP.NET Core (no sync context) so no deadlock.
    // Why throw (not IGenericResult): IConfigureOptions<T>.Configure is a void framework hook with no
    // result-returning contract — fail-loud here means throwing, never registering unsigned options.
#pragma warning disable VSTHRD002
    public void Configure(OpenIddictServerOptions options)
    {
        using var scope = _scopeFactory.CreateScope();

        var activeResult = ResolveActiveConfig(scope.ServiceProvider, CancellationToken.None).GetAwaiter().GetResult();
        // Why: OpenIddict is registered ⇒ a config MUST exist. If it isn't set, fail loud — never fall
        // open to OpenIddict's unsigned defaults. ResolveActiveConfig already logged the Critical reason.
        if (!activeResult.IsSuccess)
            throw new InvalidOperationException(
                "OpenIddict is registered but no enabled OpenIddict token manager configuration exists in ConfigurationDb "
                + "(auth.TokenManager with ServiceOptionType='OpenIddict'). The auth server cannot sign or "
                + $"validate tokens. Reason: {activeResult.CurrentMessage}");
        var (header, typed) = activeResult.Value;

        // Why: pin the explicit issuer (from the configured Authority) so token issuance and cross-service
        // validation agree on it. Resource servers (etl/scheduler) otherwise derive the issuer from their
        // own request URL and reject the auth server's tokens (ID2088). A missing/relative Authority fails.
        if (!Uri.TryCreate(typed.Authority, UriKind.Absolute, out var issuer))
            throw new InvalidOperationException(
                $"OpenIddict configuration '{header.Name}' has no absolute Authority; a valid Authority is required "
                + $"to pin the token issuer. Configured value: '{typed.Authority}'.");
        options.Issuer = issuer;

        if (string.IsNullOrEmpty(header.SecretManagerName) || string.IsNullOrEmpty(header.SecretKeyName))
            throw new InvalidOperationException(
                $"OpenIddict configuration '{header.Name}' is missing SecretManagerName/SecretKeyName; "
                + "the RS256 signing key cannot be resolved.");

        var keyResult = ResolveSigningKey(scope.ServiceProvider, header.SecretManagerName, header.SecretKeyName, CancellationToken.None)
            .GetAwaiter().GetResult();
        // Why: no key ⇒ the auth server cannot sign or validate anything. Fail loud (ResolveSigningKey
        // logged the Critical reason) rather than register unsigned options.
        if (!keyResult.IsSuccess)
            throw new InvalidOperationException(
                $"OpenIddict RS256 signing key '{header.SecretKeyName}' could not be resolved from secret manager "
                + $"'{header.SecretManagerName}' for configuration '{header.Name}'. Reason: {keyResult.CurrentMessage}");
        var key = keyResult.Value!;

        // Why: Add the RS256 signing credential so OpenIddict signs tokens with our RS256 key and
        // advertises the public component at /.well-known/jwks.
        options.SigningCredentials.Add(new SigningCredentials(key, SecurityAlgorithms.RsaSha256));

        // Why: OpenIddict requires at least one ENCRYPTION key even when access-token encryption is
        // disabled (DisableAccessTokenEncryption) — encryption keys still protect authorization codes,
        // refresh tokens, and device codes. Without one, OpenIddictServerConfiguration.PostConfigure
        // throws "At least one encryption key must be registered" on the first request. Reuse the same
        // RSA key (via RSA-OAEP key wrapping + AES-256-CBC-HMAC-SHA512 content encryption) so it is stable
        // across restarts and instances rather than ephemeral.
        options.EncryptionCredentials.Add(new EncryptingCredentials(
            key,
            SecurityAlgorithms.RsaOAEP,
            SecurityAlgorithms.Aes256CbcHmacSha512));

        OpenIddictProviderLog.SigningKeyStartupLoaded(_logger, header.Name);
    }
#pragma warning restore VSTHRD002

    // Why: this configurator injects (via the scope) only the two config providers it needs — the
    // header provider (auth.TokenManager) and the OpenIddict typed-body provider
    // (auth.OpenIddictTokenManager) — and selects the single enabled OpenIddict row itself.
    // No shared cross-consumer helper: each consumer that needs this config resolves it inline.
    private async Task<IGenericResult<(TokenManagerConfiguration Header, OpenIddictTokenManagerConfiguration Typed)>> ResolveActiveConfig(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var allHeaders = await services.GetRequiredService<TokenManagerConfigurationProvider>()
            .Get(cancellationToken).ConfigureAwait(false);
        if (!allHeaders.IsSuccess)
            return GenericResult<(TokenManagerConfiguration, OpenIddictTokenManagerConfiguration)>.Failure(
                OpenIddictProviderLog.SigningKeyStartupConfigLoadFailed(
                    _logger, allHeaders.CurrentMessage ?? "gateway returned failure with no message"));

        var header = allHeaders.Value?
            .FirstOrDefault(c => string.Equals(c.ServiceOptionType, "OpenIddict", StringComparison.OrdinalIgnoreCase));
        if (header is null)
            return GenericResult<(TokenManagerConfiguration, OpenIddictTokenManagerConfiguration)>.Failure(
                OpenIddictProviderLog.SigningKeyStartupNoConfig(_logger));

        var typedResult = await services.GetRequiredService<OpenIddictTokenManagerConfigurationProvider>()
            .Get(header.Id, cancellationToken).ConfigureAwait(false);
        if (!typedResult.IsSuccess || typedResult.Value is not OpenIddictTokenManagerConfiguration typed)
            return GenericResult<(TokenManagerConfiguration, OpenIddictTokenManagerConfiguration)>.Failure(
                OpenIddictProviderLog.SigningKeyStartupConfigLoadFailed(
                    _logger, typedResult.CurrentMessage ?? "OpenIddict typed body load returned no config"));

        return GenericResult<(TokenManagerConfiguration, OpenIddictTokenManagerConfiguration)>.Success((header, typed));
    }

    // Why: this configurator injects (via the scope) only the secret-manager provider it needs — it
    // knows just the secret manager NAME and secret NAME from the resolved header, looks the manager up
    // by name, and executes a GetSecretManagerCommand for the key material. RSA.ImportFromPem is this
    // consumer's own logic (no shared PEM-parsing helper).
    private async Task<IGenericResult<RsaSecurityKey>> ResolveSigningKey(
        IServiceProvider services,
        string secretManagerName,
        string secretKeyName,
        CancellationToken cancellationToken)
    {
        OpenIddictProviderLog.SigningKeyLoadStarted(_logger, secretManagerName, secretKeyName);

        var managerResult = await services
            .GetRequiredService<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>()
            .Get(secretManagerName, cancellationToken).ConfigureAwait(false);
        if (!managerResult.IsSuccess || managerResult.Value is null)
            return GenericResult<RsaSecurityKey>.Failure(
                OpenIddictProviderLog.SigningKeyManagerNotFound(_logger, secretManagerName));

        var secretResult = await managerResult.Value
            .Execute(GetSecretManagerCommand.Latest(container: null, secretKey: secretKeyName), cancellationToken)
            .ConfigureAwait(false);
        if (!secretResult.IsSuccess)
        {
            // Why: guard the nullable reason to a non-null string, then fail loud whether or not the
            // secret manager supplied a reason — no null-forgiving, no silent drop.
            var reason = secretResult.CurrentMessage ?? string.Empty;
            return GenericResult<RsaSecurityKey>.Failure(
                OpenIddictProviderLog.SigningKeyLoadFailed(
                    _logger, secretManagerName, secretKeyName,
                    string.IsNullOrEmpty(reason) ? "secret manager returned a failure with no message" : reason));
        }

        if (secretResult.Value is not SecretValue secret)
            return GenericResult<RsaSecurityKey>.Failure(
                OpenIddictProviderLog.SigningKeyMissing(_logger, secretManagerName, secretKeyName));

        try
        {
            using (secret)
            {
                const string keyId = "fdw-rs256-1";
                var rsa = RSA.Create();
                rsa.ImportFromPem(secret.GetStringValue().AsSpan());
                var key = new RsaSecurityKey(rsa) { KeyId = keyId };
                OpenIddictProviderLog.SigningKeyLoaded(_logger, secretManagerName, secretKeyName, keyId);
                return GenericResult<RsaSecurityKey>.Success(key);
            }
        }
        // Why: RSA.Create()/ImportFromPem's documented failure modes — malformed/corrupt PEM content
        // (CryptographicException) or an empty/invalid span (ArgumentException) — caught specifically
        // so the logged reason names the actual cause. The result carries the FULL exception chain via
        // FlattenException so a caller never sees just "One or more errors occurred."
        catch (CryptographicException ex)
        {
            OpenIddictProviderLog.SigningKeyParseFailed(_logger, ex, secretKeyName, ex.Message);
            return GenericResult<RsaSecurityKey>.Failure(ExceptionResultExtensions.FlattenException(ex));
        }
        catch (ArgumentException ex)
        {
            OpenIddictProviderLog.SigningKeyParseFailed(_logger, ex, secretKeyName, ex.Message);
            return GenericResult<RsaSecurityKey>.Failure(ExceptionResultExtensions.FlattenException(ex));
        }
    }
}
