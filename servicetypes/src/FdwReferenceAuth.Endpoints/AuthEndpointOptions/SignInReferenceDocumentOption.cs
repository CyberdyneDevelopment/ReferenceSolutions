using Fdw.Collections.Attributes;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>
/// Declares the sign-in page's document — GET /openapi/sign-in.
/// </summary>
/// <remarks>Auto-discovery is off, so this declaration is what routes the endpoint.</remarks>
[TypeOption(typeof(AuthEndpoints), "SignInReferenceDocument")]
public class SignInReferenceDocumentOption : AuthEndpointBase<SignInReferenceDocumentEndpoint>
{
}
