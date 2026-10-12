using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace FdwReferenceAuthentication.OidcRedirect.Authentication;

/// <summary>
/// TypeCollection of the ways this platform authenticates itself at an OIDC provider's token endpoint.
/// </summary>
/// <remarks>
/// <c>None</c> is a public client: the code exchange is proved by PKCE alone and no client secret is
/// sent. <c>ClientSecret</c> is a confidential client: the exchange also sends a secret read from a
/// named secret manager. The authority row chooses one by name, as a connection chooses its
/// authentication type. The source generator populates ByName(), ById(), All() and NotFound().
/// </remarks>
[TypeCollection(
    typeof(OidcClientAuthenticationConfiguration),
    typeof(OidcClientAuthenticationConfiguration),
    typeof(OidcClientAuthenticationTypes))]
public abstract partial class OidcClientAuthenticationTypes
    : TypeCollectionBase<OidcClientAuthenticationConfiguration, OidcClientAuthenticationConfiguration>
{
    /// <summary>Lists the names an authority row may declare, for messages that say what was expected.</summary>
    /// <returns>The names, comma separated.</returns>
    public static string KnownNames() => string.Join(", ", All().Select(option => option.Name));
}
