using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using FdwReferenceAuthentication.OidcRedirect.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FdwReferenceAuthentication.OidcRedirect.Registration;

/// <summary>
/// Sends a caller to an OIDC provider and turns what comes back into a subject.
/// </summary>
/// <remarks>
/// <para>
/// Runs twice. On the first call it has no authorization code, so it returns a challenge: the
/// destination and a single-use resume token the runner stores alongside everything established so
/// far. When the caller returns, the callback resumes the flow, this step exchanges the code, and
/// verifies the token it gets back.
/// </para>
/// <para>
/// Authorization code with PKCE, always - RFC 9700 requires it of confidential clients too, not only
/// public ones. The implicit grant this replaces is prohibited outright.
/// </para>
/// <para>
/// How the host authenticates at the token endpoint is the authority row's
/// <c>ClientAuthenticationType</c>, resolved the way a connection resolves its authentication type.
/// <c>None</c> is a public client - Entra with PKCE - and the code exchange sends no
/// <c>client_secret</c>. <c>ClientSecret</c> is a confidential client: the exchange carries the secret
/// the row's <c>SecretManagerName</c> and <c>SecretKeyName</c> properties name, read through that
/// secret manager for each exchange and never kept on this option, which lives as long as the
/// process. A provider that requires a secret, such as Authentik, refuses an exchange without one with
/// <c>invalid_client</c>.
/// </para>
/// <para>
/// The mechanism is identical for Auth0, Entra or Authentik - only the issuer, endpoints, audiences
/// and algorithms differ, and that is why the step lives here rather than in the framework: the
/// decision that THIS deployment sends callers to THIS provider on THESE terms is inseparable from
/// reading it. A host that never references this package cannot name <c>OidcRedirect</c> in a flow,
/// which is the intended outcome rather than an oversight.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(AuthenticationStepTypes), "OidcRedirect")]
public sealed class OidcRedirectStepType
    : AuthenticationStepTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>,
      IAuthenticationStep,
      IFederatingAuthenticationStep
{
    // Captured when the host is built: an option is created by its module initializer, which needs
    // a parameterless constructor, so what it needs arrives where a live container exists.
    private OidcAuthorityReader? _authorities;
    private ISigningKeyProvider? _keys;
    private IOidcCallbackAccessor? _callback;
    private IAuthorizationRequestStore? _requests;
    private ISecretManagerProvider? _secretManagers;
    private HttpClient? _http;
    private ILogger _logger = NullLogger<OidcRedirectStepType>.Instance;

    // Set from the authority when the step runs, mirroring ForeignTokenStepType - the runner reads
    // AuthenticationMethods only after Execute has returned.
    private IReadOnlyList<string> _asserted = [];

    /// <summary>Initializes a new instance of the <see cref="OidcRedirectStepType"/> class.</summary>
    public OidcRedirectStepType()
        : base("OidcRedirect",
               "OidcAuthorities",
               "OIDC Redirect",
               "Sends a caller to an OIDC provider and turns what comes back into a subject")
    {
        Registration((builder, loggerFactory) =>
        {
            // The authority row THIS deployment declares. On the auth path beside the flow it
            // serves, so a reader looking for what a flow trusts finds it next to the flow.
            builder.Services.AddSingleton<IOidcAuthorityImplementationConfigurationProvider, OidcAuthorityImplementationConfigurationProvider>(sp => new OidcAuthorityImplementationConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<OidcAuthorityImplementationConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                AuthenticationStepTypes.ConfigurationConnection, "auth", "OidcAuthorityConfiguration"));

            builder.Services.TryAddSingleton<OidcAuthorityConfigurationProvider>(sp =>
            {
                var domain = new OidcAuthorityConfigurationProvider(
                    sp.GetRequiredService<ILogger<OidcAuthorityConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    AuthenticationStepTypes.ConfigurationConnection, "auth", "OidcAuthority");
                domain.Register("OidcAuthority", sp.GetRequiredService<IOidcAuthorityImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IOidcAuthorityConfigurationProvider>(sp => sp.GetRequiredService<OidcAuthorityConfigurationProvider>());

            // What the step reads its authority through. Not a provider -- it states what a usable
            // authority row is, and that judgement is this package's business.
            builder.Services.TryAddScoped<OidcAuthorityReader>();

            // Calls the provider's token endpoint. Named so multiple HttpClient consumers in the
            // host don't collide on the default client's configuration.
            builder.Services.AddHttpClient(nameof(OidcRedirectStepType));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, loggerFactory) =>
        {
            _authorities = host.Services.GetRequiredService<OidcAuthorityReader>();
            _keys = host.Services.GetRequiredService<ISigningKeyProvider>();
            _callback = host.Services.GetRequiredService<IOidcCallbackAccessor>();
            _requests = host.Services.GetRequiredService<IAuthorizationRequestStore>();
            _secretManagers = host.Services.GetRequiredService<ISecretManagerProvider>();
            _http = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OidcRedirectStepType));
            _logger = loggerFactory?.CreateLogger<OidcRedirectStepType>()
                ?? NullLogger<OidcRedirectStepType>.Instance;

            return GenericResult<IHost>.Success(host);
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Requires => [];

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Contributes => [ContextElements.Subject, ContextElements.Claims];

    /// <inheritdoc />
    /// <remarks>
    /// What the authority read during <see cref="Execute"/> permits, mirroring ForeignTokenStepType:
    /// empty until the step has run, so a step declaring nothing proves nothing.
    /// </remarks>
    public IReadOnlyList<string> AuthenticationMethods => _asserted;

    /// <inheritdoc />
    /// <remarks>
    /// The authority's own row, read through the same provider <see cref="Execute"/> reads. A caller
    /// choosing where to sign in and a caller being sent there therefore cannot disagree about which
    /// provider that is - the button is labelled from the row that decides the destination.
    /// </remarks>
    public async Task<IGenericResult<string>> GetAuthorityName(CancellationToken cancellationToken = default)
    {
        if (_authorities is not { } authorities)
            return GenericResult<string>.Failure(OidcLog.NotInitialized(_logger, Name));

        var authority = await authorities.Get(cancellationToken).ConfigureAwait(false);
        return authority.IsSuccess && authority.Value is { } configuration
            ? GenericResult<string>.Success(configuration.Name)
            : authority.ToNewResult<string>();
    }

    /// <inheritdoc />
    /// <remarks>
    /// The same row <see cref="Execute"/> reads, so the issuer a binding is written with and the
    /// issuer a subject arrives carrying cannot disagree — they are the one value.
    /// </remarks>
    public async Task<IGenericResult<string>> GetIssuer(CancellationToken cancellationToken = default)
    {
        if (_authorities is not { } authorities)
            return GenericResult<string>.Failure(OidcLog.NotInitialized(_logger, Name));

        var authority = await authorities.Get(cancellationToken).ConfigureAwait(false);
        return authority.IsSuccess && authority.Value is { } configuration
            ? GenericResult<string>.Success(configuration.Issuer)
            : authority.ToNewResult<string>();
    }

    /// <inheritdoc />
    public async Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, ITenantProvider tenants, CancellationToken cancellationToken = default)
    {
        if (_authorities is not { } authorities || _keys is not { } keys || _callback is not { } callback
            || _requests is not { } requests || _secretManagers is not { } secretManagers || _http is not { } http)
            return GenericResult<StepOutcome>.Failure(OidcLog.NotInitialized(_logger, Name));

        var authority = await authorities.Get(cancellationToken).ConfigureAwait(false);
        if (!authority.IsSuccess || authority.Value is null)
            return authority.ToNewResult<StepOutcome>();

        var configuration = authority.Value;
        _asserted = configuration.AssertableMethods;

        return callback.Code is { Length: > 0 } code
            ? await Complete(configuration, keys, callback, requests, secretManagers, http, code, cancellationToken)
                .ConfigureAwait(false)
            : Begin(configuration, requests);
    }

    private IGenericResult<StepOutcome> Begin(
        IOidcAuthorityConfiguration configuration, IAuthorizationRequestStore requests)
    {
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var state = Base64Url(RandomNumberGenerator.GetBytes(32));
        var nonce = Base64Url(RandomNumberGenerator.GetBytes(32));

        var stored = requests.Store(state, new AuthorizationRequest
        {
            CodeVerifier = verifier,
            Nonce = nonce,
            Issuer = configuration.Issuer,
        });

        if (stored.IsFailure)
            return stored.ToNewResult<StepOutcome>();

        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

        var destination = new UriBuilder(configuration.AuthorizationEndpoint)
        {
            Query = string.Join('&',
            [
                "response_type=code",
                $"client_id={Uri.EscapeDataString(configuration.ClientId)}",
                $"redirect_uri={Uri.EscapeDataString(configuration.RedirectUri.AbsoluteUri)}",
                $"scope={Uri.EscapeDataString(string.Join(' ', configuration.Scopes))}",
                $"state={Uri.EscapeDataString(state)}",
                $"nonce={Uri.EscapeDataString(nonce)}",
                $"code_challenge={Uri.EscapeDataString(challenge)}",
                "code_challenge_method=S256",
            ]),
        }.Uri;

        OidcLog.Challenging(_logger, configuration.Issuer);

        return GenericResult<StepOutcome>.Success(new StepOutcome.Challenge(destination, state));
    }

    private async Task<IGenericResult<StepOutcome>> Complete(
        IOidcAuthorityConfiguration configuration,
        ISigningKeyProvider keyProvider,
        IOidcCallbackAccessor callback,
        IAuthorizationRequestStore requests,
        ISecretManagerProvider secretManagers,
        HttpClient http,
        string code,
        CancellationToken cancellationToken)
    {
        if (callback.State is not { Length: > 0 } state)
            return GenericResult<StepOutcome>.Failure(OidcLog.StateMissing(_logger, configuration.Issuer));

        var request = requests.TryConsume(state);
        if (request.IsFailure)
            return request.ToNewResult<StepOutcome>();

        if (!string.Equals(request.Value!.Issuer, configuration.Issuer, StringComparison.Ordinal))
            return GenericResult<StepOutcome>.Failure(
                OidcLog.IssuerMismatch(_logger, configuration.Issuer, request.Value!.Issuer));

        var exchanged = await Exchange(configuration, secretManagers, http, code, request.Value!.CodeVerifier, cancellationToken)
            .ConfigureAwait(false);
        if (exchanged.IsFailure)
            return exchanged.ToNewResult<StepOutcome>();

        var keys = await keyProvider.Current(configuration.JwksUri, cancellationToken).ConfigureAwait(false);
        if (keys.IsFailure)
            return keys.ToNewResult<StepOutcome>();

        var validated = await new JsonWebTokenHandler().ValidateTokenAsync(exchanged.Value!,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration.Issuer,
                ValidateAudience = true,
                ValidAudiences = configuration.ValidAudiences,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keys.Value,
                ValidAlgorithms = configuration.ValidAlgorithms,
                ClockSkew = configuration.ClockSkew,
            }).ConfigureAwait(false);

        if (!validated.IsValid)
            return GenericResult<StepOutcome>.Failure(OidcLog.TokenRejected(
                _logger, configuration.Issuer, validated.Exception?.GetType().Name ?? "unknown"));

        var nonce = validated.ClaimsIdentity.FindFirst("nonce")?.Value;
        if (!string.Equals(nonce, request.Value!.Nonce, StringComparison.Ordinal))
            return GenericResult<StepOutcome>.Failure(OidcLog.NonceMismatch(_logger, configuration.Issuer));

        var subjectId = validated.ClaimsIdentity.FindFirst(configuration.SubjectClaim)?.Value;
        if (string.IsNullOrWhiteSpace(subjectId))
            return GenericResult<StepOutcome>.Failure(
                OidcLog.SubjectClaimMissing(_logger, configuration.Issuer, configuration.SubjectClaim));

        OidcLog.Completed(_logger, configuration.Issuer);

        return GenericResult<StepOutcome>.Success(new StepOutcome.Contributed(new ContextContribution
        {
            ObservedMethods = [.. validated.ClaimsIdentity.FindAll("amr").Select(c => c.Value)],

            Subject = new Subject
            {
                Issuer = configuration.Issuer,
                SubjectId = subjectId,
                AuthenticatedAt = DateTimeOffset.UtcNow,
            },
            Claims = [.. validated.ClaimsIdentity.Claims
                .Where(c => c.Type is not ("sub" or "aud" or "iss" or "exp" or "nbf" or "iat" or "nonce"))
                .Select(c => new Claim
                {
                    Type = c.Type,
                    Value = c.Value,
                    Source = ClaimSources.External,
                    Issuer = configuration.Issuer,
                })],
        }));
    }

    private async Task<IGenericResult<string>> Exchange(
        IOidcAuthorityConfiguration configuration,
        ISecretManagerProvider secretManagers,
        HttpClient http,
        string code,
        string verifier,
        CancellationToken cancellationToken)
    {
        var clientAuthentication = OidcClientAuthenticationTypes.ByName(configuration.ClientAuthenticationType ?? string.Empty);
        if (clientAuthentication.IsEmpty)
            return GenericResult<string>.Failure(OidcAuthorityLog.ClientAuthenticationTypeUnknown(
                _logger, configuration.Name, configuration.ClientAuthenticationType ?? string.Empty,
                OidcClientAuthenticationTypes.KnownNames()));

        var required = clientAuthentication.RequiredSecret(configuration.ClientAuthenticationProperties);
        if (required.IsFailure)
            return required.ToNewResult<string>();

        var form = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "authorization_code"),
            new("code", code),
            // AbsoluteUri, as the authorize request sent it: ToString() undoes percent-encoding, and the provider
            // compares redirect_uri exactly (FDW-1037).
            new("redirect_uri", configuration.RedirectUri.AbsoluteUri),
            new("client_id", configuration.ClientId),
            new("code_verifier", verifier),
        };

        // A type that names no secret is a public client: PKCE alone proves the exchange, so no
        // client_secret is sent. One that names a secret sends it client_secret_post, as
        // ClientCredentialsIdentityService does: Auth0, Entra and Authentik all accept it in the form.
        if (!required.Value.IsEmpty)
        {
            var clientSecret = await ReadClientSecret(configuration, required.Value, secretManagers, cancellationToken)
                .ConfigureAwait(false);
            if (clientSecret.IsFailure)
                return clientSecret;

            form.Add(new("client_secret", clientSecret.Value!));
        }

        try
        {
            using var response = await http
                .PostAsync(configuration.TokenEndpoint, new FormUrlEncodedContent(form), cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return GenericResult<string>.Failure(OidcLog.ExchangeRefused(
                    _logger, configuration.Issuer,
                    ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture)));

            using var document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

            return document.RootElement.TryGetProperty("id_token", out var token)
                && token.GetString() is { Length: > 0 } value
                ? GenericResult<string>.Success(value)
                : GenericResult<string>.Failure(OidcLog.NoIdToken(_logger, configuration.Issuer));
        }
        catch (HttpRequestException ex)
        {
            return GenericResult<string>.Failure(
                OidcLog.ExchangeFailed(_logger, configuration.Issuer, ex.GetType().Name));
        }
    }

    /// <summary>Reads the client secret the token endpoint authenticates this host with.</summary>
    /// <remarks>
    /// Read for each exchange and never kept on this option, as ClientCredentialsIdentityService does:
    /// the option lives as long as the process, and so would a secret held in one of its fields.
    /// </remarks>
    private async Task<IGenericResult<string>> ReadClientSecret(
        IOidcAuthorityConfiguration configuration,
        SecretRequirement requirement,
        ISecretManagerProvider secretManagers,
        CancellationToken cancellationToken)
    {
        var manager = await secretManagers.Get(requirement.ManagerName, cancellationToken).ConfigureAwait(false);
        if (!manager.IsSuccess || manager.Value is null)
            return manager.ToNewResult<string>();

        var secret = await manager.Value
            .Execute(GetSecretManagerCommand.Latest(container: null, secretKey: requirement.KeyName), cancellationToken)
            .ConfigureAwait(false);
        if (!secret.IsSuccess || secret.Value is null)
            return secret.ToNewResult<string>();

        using var value = secret.Value;
        if (value.GetStringValue() is not { Length: > 0 } clientSecret)
            return GenericResult<string>.Failure(OidcLog.ClientSecretEmpty(
                _logger, configuration.Issuer, requirement.KeyName, requirement.ManagerName));

        OidcLog.ClientSecretResolved(_logger, configuration.Issuer, requirement.ManagerName);

        return GenericResult<string>.Success(clientSecret);
    }

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
