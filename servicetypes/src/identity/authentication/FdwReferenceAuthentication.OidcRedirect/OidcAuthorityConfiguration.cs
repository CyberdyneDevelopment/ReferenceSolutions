using Fdw.Services.Authentication.Abstractions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using FdwReferenceAuthentication.OidcRedirect.Authentication;

namespace FdwReferenceAuthentication.OidcRedirect;

/// <summary>
/// An OIDC provider this host will redirect a caller to, and the terms it trusts it on.
/// </summary>
/// <remarks>
/// The row behind an <c>OidcRedirect</c> step. It names one provider - Auth0, Entra, Authentik - and
/// what this host requires of the tokens it returns. Everything here is required except the clock
/// skew: a missing audience list or algorithm list is the kind of absence that reads as "trust more",
/// so it fails rather than widening. How this host authenticates at the provider's token endpoint is
/// declared the way a connection declares its authentication: a type (<c>None</c> for a public client,
/// <c>ClientSecret</c> for a confidential one) and the key/value set that type reads.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "OidcAuthority")]
public partial class OidcAuthorityConfiguration : IOidcAuthorityImplementationConfiguration
{
    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;


    /// <summary>Initializes a new instance of the <see cref="OidcAuthorityConfiguration"/> class.</summary>
    public OidcAuthorityConfiguration()
    {
    }

    /// <summary>Gets or sets the row identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name a flow step refers to this provider by.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the issuer a token must name in its <c>iss</c> claim.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Gets or sets the provider's authorization endpoint.</summary>
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    /// <summary>Gets or sets the provider's token endpoint.</summary>
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>Gets or sets where the provider publishes its signing keys.</summary>
    public string JwksUri { get; set; } = string.Empty;

    /// <summary>Gets or sets the client identifier this platform registered with the provider.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Gets or sets how this platform authenticates itself at the provider's token endpoint.</summary>
    /// <remarks>
    /// Names an option of <see cref="OidcClientAuthenticationTypes"/>. Empty, never a chosen default, is
    /// the "not yet supplied" marker, and the reader refuses a row that carries it.
    /// </remarks>
    [ValuesFrom(typeof(OidcClientAuthenticationTypes))]
    public string ClientAuthenticationType { get; set; } = string.Empty;

    /// <summary>Gets or sets the key/value set the client authentication type reads.</summary>
    /// <remarks>
    /// [NotMapped] because the values live in the <c>OidcAuthorityProperty</c> child table, not a column
    /// on this row. Values are names - a secret manager and a key within it - never secret values: a
    /// secret in a configuration row is a secret in every backup.
    /// </remarks>
    [NotMapped]
    [ConfigurationChildTable("OidcAuthorityProperty")]
    public IDictionary<string, string?> AdditionalProperties { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets the redirect the provider returns the caller to.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Gets or sets the scopes requested, comma separated.</summary>
    public string Scopes { get; set; } = string.Empty;

    /// <summary>Gets or sets the claim carrying the subject identifier to bind on.</summary>
    /// <remarks><c>sub</c> for most providers; Entra needs <c>oid</c> - see the interface remarks.</remarks>
    public string SubjectClaim { get; set; } = string.Empty;

    /// <summary>Gets or sets the audiences a token may name, comma separated.</summary>
    public string ValidAudiences { get; set; } = string.Empty;

    /// <summary>Gets or sets the signing algorithms accepted, comma separated.</summary>
    public string ValidAlgorithms { get; set; } = string.Empty;

    /// <summary>Gets or sets the authentication methods this provider may assert, comma separated.</summary>
    public string AssertableMethods { get; set; } = string.Empty;

    /// <summary>Gets or sets the tolerated clock difference, in seconds.</summary>
    public int ClockSkewSeconds { get; set; }

    /// <summary>Gets or sets whether this row is the current version.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether this row is deleted.</summary>
    public bool IsDeleted { get; set; }
}
