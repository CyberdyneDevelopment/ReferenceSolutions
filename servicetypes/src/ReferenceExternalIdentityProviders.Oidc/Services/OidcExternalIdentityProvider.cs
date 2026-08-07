using System;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Fdw.Services.ExternalIdentityProviders.Oidc;

/// <summary>
/// Reference <see cref="IExternalIdentityProvider"/> — validates a standard OIDC JWT against signing
/// keys fetched from the configured Authority's discovery document/JWKS. Validates signature, issuer,
/// audience, and lifetime. Fails loud (never a fallback) on missing Authority/ClientId or any
/// validation failure. This provider needs no client secret — JWKS-based signature validation is
/// asymmetric (public key) — so <see cref="Fdw.Services.ExternalIdentityProviders.ExternalIdentityProviderConfiguration.SecretManagerName"/>/
/// <c>SecretKeyName</c> are left unset for this ServiceOptionType; a FUTURE provider type that DOES
/// need a shared secret resolves it the same way every other FDW domain does — via
/// <c>ISecretManager</c> from those two header fields, never a plaintext config column.
/// </summary>
public sealed class OidcExternalIdentityProvider : IExternalIdentityProvider
{
    private readonly ExternalIdentityProviderConfiguration _header;
    private readonly OidcExternalIdentityProviderConfiguration _typed;
    private readonly ILogger<OidcExternalIdentityProvider> _logger;
    private readonly Lazy<IConfigurationManager<OpenIdConnectConfiguration>> _configurationManager;

    /// <summary>Initializes a new instance of the <see cref="OidcExternalIdentityProvider"/> class.</summary>
    /// <param name="header">The header configuration row.</param>
    /// <param name="typed">The composed Oidc typed-body configuration.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="configurationManager">
    /// Test seam — supplies a fake/mocked discovery source so unit tests can validate JWTs without a
    /// live network call. Production callers (the factory) never set this; it is built lazily from
    /// <see cref="OidcExternalIdentityProviderConfiguration.Authority"/>/<c>MetadataAddress</c>.
    /// </param>
    public OidcExternalIdentityProvider(
        ExternalIdentityProviderConfiguration header,
        OidcExternalIdentityProviderConfiguration typed,
        ILogger<OidcExternalIdentityProvider>? logger,
        IConfigurationManager<OpenIdConnectConfiguration>? configurationManager = null)
    {
        _header = header ?? throw new ArgumentNullException(nameof(header));
        _typed = typed ?? throw new ArgumentNullException(nameof(typed));
        _logger = logger ?? NullLogger<OidcExternalIdentityProvider>.Instance;

        // Why: lazy — Authority may be missing (ValidateExternalToken fails loud on that before this
        // is ever touched), so building the discovery address eagerly in the constructor risks a
        // NullReferenceException on a config row that simply hasn't been fully configured yet.
        _configurationManager = new Lazy<IConfigurationManager<OpenIdConnectConfiguration>>(
            () => configurationManager ?? BuildConfigurationManager());
    }

    // ── IGenericService ────────────────────────────────────────────────────────────
    // Why: IExternalIdentityProvider's surface is the direct ValidateExternalToken verb, not a
    // command-dispatch model — mirrors how OpenIdTokenManager satisfies the same base-interface
    // obligation for ITokenManager.

    /// <inheritdoc cref="IGenericService.Id" />
    public string Id => _header.Id.ToString();

    /// <inheritdoc />
    public string Name => _header.Name;

    /// <inheritdoc cref="IGenericService.ServiceType" />
    public string ServiceType => "Oidc";

    /// <inheritdoc cref="IGenericService.IsAvailable" />
    public bool IsAvailable => true;

    Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult(GenericResult<T>.Failure(
            ExternalIdentityProviderLog.CommandNotDispatchable(_logger, command?.CommandType ?? "(null)")));

    Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult<IGenericResult>(GenericResult.Failure(
            ExternalIdentityProviderLog.CommandNotDispatchable(_logger, command?.CommandType ?? "(null)")));

    // ── IExternalIdentityProvider ───────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IGenericResult<ClaimsPrincipal>> ValidateExternalToken(string token, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);

        var providerName = _header.Name;
        ExternalIdentityProviderLog.ValidationStarted(_logger, providerName);

        if (string.IsNullOrEmpty(_typed.Authority) || string.IsNullOrEmpty(_typed.ClientId))
            return GenericResult<ClaimsPrincipal>.Failure(
                ExternalIdentityProviderLog.ConfigurationIncomplete(_logger, providerName,
                    "Authority and ClientId are both required to validate an external token."));

        var signingKeysResult = await ResolveSigningKeys(cancellationToken).ConfigureAwait(false);
        if (!signingKeysResult.IsSuccess)
            return signingKeysResult.ToNewResult<ClaimsPrincipal>();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _typed.Authority,
            ValidateAudience = true,
            ValidAudience = _typed.Audience ?? _typed.ClientId,
            ValidateLifetime = true,
            IssuerSigningKeys = signingKeysResult.Value,
        };

        return await ValidateSignature(token, providerName, validationParameters).ConfigureAwait(false);
    }

    private async Task<IGenericResult<ClaimsPrincipal>> ValidateSignature(
        string token, string providerName, TokenValidationParameters validationParameters)
    {
        var handler = new JsonWebTokenHandler { MapInboundClaims = false };
        TokenValidationResult result;
        try
        {
            result = await handler.ValidateTokenAsync(token, validationParameters).ConfigureAwait(false);
        }
        catch (SecurityTokenException ex)
        {
            return GenericResult<ClaimsPrincipal>.Failure(
                ExternalIdentityProviderLog.ExternalTokenValidationFailed(_logger, providerName, ex.Message));
        }

        if (!result.IsValid)
            return GenericResult<ClaimsPrincipal>.Failure(
                ExternalIdentityProviderLog.ExternalTokenValidationFailed(_logger, providerName,
                    result.Exception?.Message ?? "token validation failed"));

        var principal = new ClaimsPrincipal(result.ClaimsIdentity);
        ExternalIdentityProviderLog.ValidationSucceeded(_logger, providerName,
            principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "(none)");
        return GenericResult<ClaimsPrincipal>.Success(principal);
    }

    // Why: fetch (and internally cache/auto-refresh, via ConfigurationManager) the Authority's JWKS.
    // A discovery/network failure is a structured, logged failure — never a defaulted/empty key set.
    private async Task<IGenericResult<System.Collections.Generic.IEnumerable<SecurityKey>>> ResolveSigningKeys(CancellationToken cancellationToken)
    {
        try
        {
            var discoveryConfig = await _configurationManager.Value.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
            return GenericResult<System.Collections.Generic.IEnumerable<SecurityKey>>.Success(discoveryConfig.SigningKeys);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            return GenericResult<System.Collections.Generic.IEnumerable<SecurityKey>>.Failure(
                ExternalIdentityProviderLog.ExternalTokenValidationFailed(_logger, _header.Name,
                    $"failed to load discovery document/signing keys: {ex.Message}"));
        }
    }

    private ConfigurationManager<OpenIdConnectConfiguration> BuildConfigurationManager()
    {
        var metadataAddress = string.IsNullOrEmpty(_typed.MetadataAddress)
            ? _typed.Authority!.TrimEnd('/') + "/.well-known/openid-configuration"
            : _typed.MetadataAddress;

        return new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = true });
    }
}
