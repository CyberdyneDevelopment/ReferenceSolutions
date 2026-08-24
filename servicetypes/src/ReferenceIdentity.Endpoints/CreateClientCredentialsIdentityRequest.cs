using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Identity.Endpoints;

namespace ReferenceIdentity.Endpoints;

/// <summary>
/// Creates an identity that authenticates with a client id and secret.
/// </summary>
/// <remarks>
/// Issuer and TokenEndpoint are separate because they differ in shape: the issuer is per
/// application (<c>/application/o/&lt;slug&gt;/</c>) and the token endpoint is instance-wide
/// (<c>/application/o/token/</c>). Pointing the token endpoint at the per-application path returns
/// 404 at first acquisition, long after this call succeeded.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class CreateClientCredentialsIdentityRequest : CreateIdentityRequest
{
    /// <summary>Gets or sets the issuer that mints the token.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Gets or sets the instance-wide token endpoint.</summary>
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>Gets or sets the OAuth client id of the provider's application.</summary>
    /// <remarks>A public identifier, not a credential; the secret is named, never carried.</remarks>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Gets or sets the space-separated scopes to request.</summary>
    public string? Scopes { get; set; }
}
