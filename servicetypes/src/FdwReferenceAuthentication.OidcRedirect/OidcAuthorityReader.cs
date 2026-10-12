using System.Collections.ObjectModel;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Steps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceAuthentication.OidcRedirect.Authentication;

namespace FdwReferenceAuthentication.OidcRedirect;

/// <summary>
/// Builds what an <c>OidcRedirect</c> step sends a caller to, from the authority row.
/// </summary>
/// <remarks>
/// The step reads its authority through <see cref="IOidcAuthorityConfigurationProvider"/>, and this
/// is the implementation of it: it reads the single declared authority row and converts the
/// delimited columns into the lists the step wants. This is why the vendor's own values live in this
/// package and the step lives in the framework: challenging with PKCE, exchanging the code and
/// verifying the returned token are identical for Auth0, Entra and Authentik, and only these rows
/// differ.
/// </remarks>
/// <remarks>Not a provider: it reads through the OidcAuthority domain provider and states what a
/// usable authority row is. The validation is why it exists, so it does not belong on a provider.</remarks>
public sealed class OidcAuthorityReader
{
    private readonly IOidcAuthorityConfigurationProvider _authorities;

    private readonly ILogger _log;

    /// <summary>Initializes a new instance of the <see cref="OidcAuthorityReader"/> class.</summary>
    /// <param name="authorities">Reads the declared OIDC authorities.</param>
    /// <param name="logger">The logger.</param>
    public OidcAuthorityReader(
        IOidcAuthorityConfigurationProvider authorities,
        ILogger<OidcAuthorityReader>? logger = null)
    {
        _authorities = authorities ?? throw new ArgumentNullException(nameof(authorities));
        _log = logger ?? NullLogger<OidcAuthorityReader>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IOidcAuthorityConfiguration>> Get(
        CancellationToken cancellationToken = default)
    {
        var rows = await _authorities.Get(cancellationToken).ConfigureAwait(false);
        if (!rows.IsSuccess || rows.Value is null)
            return rows.ToNewResult<IOidcAuthorityConfiguration>();

        var live = rows.Value.Where(r => r.IsCurrent && !r.IsDeleted).ToList();

        if (live.Count == 0)
            return GenericResult<IOidcAuthorityConfiguration>.Failure(
                OidcAuthorityLog.NoneDeclared(_log));

        // More than one is refused rather than guessed at, for the same reason the ForeignAuthority
        // reader refuses it: which provider a caller is sent to is the decision this row exists to
        // record, and picking by order would make it depend on read order instead.
        if (live.Count > 1)
            return GenericResult<IOidcAuthorityConfiguration>.Failure(
                OidcAuthorityLog.Ambiguous(_log, string.Join(", ", live.Select(r => r.Name))));

        var row = live[0];

        if (string.IsNullOrWhiteSpace(row.Issuer))
            return GenericResult<IOidcAuthorityConfiguration>.Failure(
                OidcAuthorityLog.IssuerMissing(_log, row.Name));

        if (!Uri.TryCreate(row.AuthorizationEndpoint, UriKind.Absolute, out var authorizationEndpoint))
            return GenericResult<IOidcAuthorityConfiguration>.Failure(
                OidcAuthorityLog.AuthorizationEndpointUnusable(_log, row.Name, row.AuthorizationEndpoint));

        if (!Uri.TryCreate(row.TokenEndpoint, UriKind.Absolute, out var tokenEndpoint))
            return GenericResult<IOidcAuthorityConfiguration>.Failure(
                OidcAuthorityLog.TokenEndpointUnusable(_log, row.Name, row.TokenEndpoint));

        if (!Uri.TryCreate(row.JwksUri, UriKind.Absolute, out var jwks))
            return GenericResult<IOidcAuthorityConfiguration>.Failure(
                OidcAuthorityLog.JwksUriUnusable(_log, row.Name, row.JwksUri));

        if (string.IsNullOrWhiteSpace(row.ClientId))
            return GenericResult<IOidcAuthorityConfiguration>.Failure(
                OidcAuthorityLog.ClientIdMissing(_log, row.Name));

        // A blank type is refused rather than read as "public client": how this platform proves itself
        // at the token endpoint is what the row exists to record, so no default is invented.
        var clientAuthentication = string.IsNullOrWhiteSpace(row.ClientAuthenticationType)
            ? OidcClientAuthenticationTypes.NotFound
            : OidcClientAuthenticationTypes.ByName(row.ClientAuthenticationType);
        if (clientAuthentication.IsEmpty)
            return GenericResult<IOidcAuthorityConfiguration>.Failure(
                OidcAuthorityLog.ClientAuthenticationTypeUnknown(
                    _log, row.Name, row.ClientAuthenticationType ?? string.Empty, OidcClientAuthenticationTypes.KnownNames()));

        var clientAuthenticationProperties = new Dictionary<string, string?>(
            row.AdditionalProperties ?? new Dictionary<string, string?>(), StringComparer.OrdinalIgnoreCase);
        if (!clientAuthentication.Validate(clientAuthenticationProperties).IsSuccess)
            return GenericResult<IOidcAuthorityConfiguration>.Failure(
                OidcAuthorityLog.ClientAuthenticationPropertiesInvalid(
                    _log, row.Name, clientAuthentication.Name, string.Join(", ", clientAuthentication.RequiredProperties)));

        if (!Uri.TryCreate(row.RedirectUri, UriKind.Absolute, out var redirectUri))
            return GenericResult<IOidcAuthorityConfiguration>.Failure(
                OidcAuthorityLog.RedirectUriUnusable(_log, row.Name, row.RedirectUri));

        var scopes = Split(row.Scopes);
        if (scopes.Length == 0)
            return GenericResult<IOidcAuthorityConfiguration>.Failure(
                OidcAuthorityLog.ScopesMissing(_log, row.Name));

        if (string.IsNullOrWhiteSpace(row.SubjectClaim))
            return GenericResult<IOidcAuthorityConfiguration>.Failure(
                OidcAuthorityLog.SubjectClaimMissing(_log, row.Name));

        var audiences = Split(row.ValidAudiences);
        if (audiences.Length == 0)
            return GenericResult<IOidcAuthorityConfiguration>.Failure(
                OidcAuthorityLog.AudiencesMissing(_log, row.Name));

        var algorithms = Split(row.ValidAlgorithms);
        if (algorithms.Length == 0)
            return GenericResult<IOidcAuthorityConfiguration>.Failure(
                OidcAuthorityLog.AlgorithmsMissing(_log, row.Name));

        var methods = Split(row.AssertableMethods);
        if (methods.Length == 0)
            return GenericResult<IOidcAuthorityConfiguration>.Failure(
                OidcAuthorityLog.MethodsMissing(_log, row.Name));

        OidcAuthorityLog.Read(_log, row.Name, row.Issuer, audiences.Length);

        return GenericResult<IOidcAuthorityConfiguration>.Success(new OidcRedirectStepConfiguration
        {
            Name = row.Name,
            Issuer = row.Issuer,
            AuthorizationEndpoint = authorizationEndpoint,
            TokenEndpoint = tokenEndpoint,
            JwksUri = jwks,
            ClientId = row.ClientId,
            ClientAuthenticationType = clientAuthentication.Name,
            ClientAuthenticationProperties = new ReadOnlyDictionary<string, string?>(clientAuthenticationProperties),
            RedirectUri = redirectUri,
            Scopes = scopes,
            SubjectClaim = row.SubjectClaim,
            ValidAudiences = audiences,
            ValidAlgorithms = algorithms,
            AssertableMethods = methods,
            ClockSkew = TimeSpan.FromSeconds(row.ClockSkewSeconds),
        });
    }

    private static string[] Split(string value)
        => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
