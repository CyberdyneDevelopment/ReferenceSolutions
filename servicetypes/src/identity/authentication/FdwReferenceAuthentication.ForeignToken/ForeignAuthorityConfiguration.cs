using Fdw.Services.Authentication.Abstractions;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace FdwReferenceAuthentication.ForeignToken;

/// <summary>
/// A foreign authority this host will exchange a token from, and the terms it is trusted on.
/// </summary>
/// <remarks>
/// <para>
/// The row behind a <c>ForeignToken</c> step. It names one authority — an Auth0 tenant, an Entra
/// directory — and what this host requires of the tokens it signs. Separate from the authentication
/// SERVICE rows, which say which issuers a token may arrive on: this says which issuer a caller may
/// be exchanged FROM, and those are different decisions. A host can accept an authority's tokens
/// directly without letting them be exchanged for its own.
/// </para>
/// <para>
/// Everything here is required except the clock skew. A missing audience list or algorithm list is
/// the kind of absence that reads as "trust more", so it fails rather than widening.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ForeignAuthority")]
public partial class ForeignAuthorityConfiguration : IForeignAuthorityImplementationConfiguration
{
    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;


    /// <summary>Initializes a new instance of the <see cref="ForeignAuthorityConfiguration"/> class.</summary>
    public ForeignAuthorityConfiguration()
    {
    }

    /// <summary>Gets or sets the row identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name a flow step refers to this authority by.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the issuer a token must name in its <c>iss</c> claim.</summary>
    /// <remarks>The authority's full URL including scheme, matched literally against the token.</remarks>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Gets or sets where the authority publishes the keys it signs with.</summary>
    /// <remarks>
    /// Held rather than derived from <see cref="Issuer"/>: most authorities publish at
    /// <c>{issuer}/.well-known/jwks.json</c> and some do not, and guessing wrongly fails as an
    /// unreachable-keys error that reads like an outage.
    /// </remarks>
    public string JwksUri { get; set; } = string.Empty;

    /// <summary>Gets or sets the audiences a token may name, comma separated.</summary>
    /// <remarks>
    /// One column carrying a list, which is how a configuration row does that here — the same shape
    /// as <c>sec.ClientCredentialsIdentity.Scopes</c>.
    /// </remarks>
    public string ValidAudiences { get; set; } = string.Empty;

    /// <summary>Gets or sets the signing algorithms accepted, comma separated.</summary>
    /// <remarks>
    /// Pinned here rather than read from each token's header. An attacker who chooses the algorithm
    /// can choose one the key trivially satisfies, so the list is the host's decision.
    /// </remarks>
    public string ValidAlgorithms { get; set; } = string.Empty;

    /// <summary>Gets or sets the authentication methods this authority may assert, comma separated.</summary>
    /// <remarks>
    /// RFC 8176 values. What this authority is trusted to have actually verified — an authority
    /// permitted to assert <c>mfa</c> is one whose enforcement of it you have checked.
    /// </remarks>
    public string AssertableMethods { get; set; } = string.Empty;

    /// <summary>Gets or sets the tolerated clock difference, in seconds.</summary>
    public int ClockSkewSeconds { get; set; }

    /// <summary>Gets or sets whether this row is the current version.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether this row is deleted.</summary>
    public bool IsDeleted { get; set; }
}
