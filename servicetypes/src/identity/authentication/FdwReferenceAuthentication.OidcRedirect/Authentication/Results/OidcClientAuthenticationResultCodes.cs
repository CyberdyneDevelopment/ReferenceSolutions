using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace FdwReferenceAuthentication.OidcRedirect.Authentication.Results;

/// <summary>
/// TypeCollection for the result codes of OIDC client authentication.
/// Result codes use categorized catalog numbers (Code "OIDC-{number}", Id == EventId == number).
/// </summary>
[TypeCollection(typeof(OidcClientAuthenticationResultCodeBase), typeof(IResultCode), typeof(OidcClientAuthenticationResultCodes))]
public abstract partial class OidcClientAuthenticationResultCodes
    : TypeCollectionBase<OidcClientAuthenticationResultCodeBase, IResultCode>
{
}
