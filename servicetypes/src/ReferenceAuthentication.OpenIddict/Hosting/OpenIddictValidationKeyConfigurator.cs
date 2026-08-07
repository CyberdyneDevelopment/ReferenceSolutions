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
using OpenIddict.Abstractions;
using OpenIddict.Validation;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Hosting;

/// <summary>
/// Configures <see cref="OpenIddictValidationOptions"/> with the explicit issuer and RS256 signing
/// credential for a VALIDATION-ONLY host (no co-resident <c>AddServer()</c>) — resolved on demand
/// from the gateway-backed configuration providers and the secret manager, exactly like
/// <see cref="OpenIddictSigningKeyConfigurator"/> does for the issuance side.
/// </summary>
/// <remarks>
/// Replaces <c>UseLocalServer()</c> (from <c>OpenIddict.Validation.ServerIntegration</c>), which only
/// works when <c>AddServer()</c> is registered in the SAME container and auto-wires the validation
/// options from the server's own <c>OpenIddictServerOptions</c>. A
/// validation-only host (<c>AuthenticationServerTypes</c>) has no local server, so this configurator
/// supplies <see cref="OpenIddictValidationOptions.Issuer"/> and
/// <see cref="OpenIddictValidationOptions.SigningCredentials"/> directly. OpenIddict's validation
/// pipeline defaults to local (in-process) JWT validation regardless — <c>UseLocalServer()</c> is
/// purely a convenience for sourcing the key/issuer from a co-resident server, not a different
/// validation mode. Config + key resolution are inlined here (no shared helper) — this configurator
/// injects only the providers IT needs.
/// </remarks>
internal sealed class OpenIddictValidationKeyConfigurator : IConfigureOptions<OpenIddictValidationOptions>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OpenIddictValidationKeyConfigurator> _logger;

    public OpenIddictValidationKeyConfigurator(
        IServiceScopeFactory scopeFactory,
        ILoggerFactory? loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        _scopeFactory = scopeFactory;
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<OpenIddictValidationKeyConfigurator>();
    }

    /// <inheritdoc />
    // Why: sync-over-async (.GetAwaiter().GetResult()) because IConfigureOptions.Configure is a
    // synchronous framework seam. Resolved once at options-build via a short-lived scope, mirroring
    // OpenIddictSigningKeyConfigurator — the sanctioned FDW idiom for a sync/singleton context that
    // needs a scoped secret-backed provider.
#pragma warning disable VSTHRD002
    public void Configure(OpenIddictValidationOptions options)
    {
        using var scope = _scopeFactory.CreateScope();

        var activeResult = ResolveActiveConfig(scope.ServiceProvider, CancellationToken.None).GetAwaiter().GetResult();
        // Why: OpenIddict validation is registered ⇒ a config MUST exist. If it isn't set, fail loud —
        // never fall open to unvalidated tokens. ResolveActiveConfig already logged the Critical reason.
        if (!activeResult.IsSuccess)
            throw new InvalidOperationException(
                "OpenIddict validation is registered but no enabled OpenIddict token manager configuration exists in "
                + "ConfigurationDb (auth.TokenManager with ServiceOptionType='OpenIddict'). This "
                + $"resource server cannot validate tokens. Reason: {activeResult.CurrentMessage}");
        var (header, typed) = activeResult.Value;

        // Why: pin the explicit issuer (from the configured Authority) so validation agrees with the
        // issuing auth server. A missing/relative Authority fails loud rather than validating against
        // no issuer at all.
        if (!Uri.TryCreate(typed.Authority, UriKind.Absolute, out var issuer))
            throw new InvalidOperationException(
                $"OpenIddict configuration '{header.Name}' has no absolute Authority; a valid Authority is "
                + $"required to pin the validation issuer. Configured value: '{typed.Authority}'.");
        options.Issuer = issuer;

        if (string.IsNullOrEmpty(header.SecretManagerName) || string.IsNullOrEmpty(header.SecretKeyName))
            throw new InvalidOperationException(
                $"OpenIddict configuration '{header.Name}' is missing SecretManagerName/SecretKeyName; "
                + "the RS256 validation key cannot be resolved.");

        var keyResult = ResolveSigningKey(scope.ServiceProvider, header.SecretManagerName, header.SecretKeyName, CancellationToken.None)
            .GetAwaiter().GetResult();
        // Why: no key ⇒ this resource server cannot validate anything. Fail loud (ResolveSigningKey
        // logged the Critical reason) rather than register options with no verification key.
        if (!keyResult.IsSuccess)
            throw new InvalidOperationException(
                $"OpenIddict RS256 signing key '{header.SecretKeyName}' could not be resolved from secret "
                + $"manager '{header.SecretManagerName}' for configuration '{header.Name}'. Reason: {keyResult.CurrentMessage}");
        var key = keyResult.Value!;

        // Why: supply a STATIC OpenIddict configuration (issuer + RS256 verification key) so the
        // validation pipeline resolves the key locally and NEVER attempts OIDC server discovery over
        // HTTP. Setting options.Issuer alone leaves options.Configuration null, which makes OpenIddict
        // build a discovery ConfigurationManager and fail at first request with "A discovery client must
        // be registered when using server discovery" (this validation-only host has no UseSystemNetHttp).
        // Only a signing (verification) key is needed — access tokens are issued with
        // DisableAccessTokenEncryption(), so this host never needs to decrypt anything.
        var staticConfiguration = new OpenIddictConfiguration { Issuer = issuer };
        staticConfiguration.SigningKeys.Add(key);
        options.Configuration = staticConfiguration;

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
