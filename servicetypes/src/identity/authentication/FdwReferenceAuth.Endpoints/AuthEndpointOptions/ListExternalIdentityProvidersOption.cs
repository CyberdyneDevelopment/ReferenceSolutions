using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>
/// Declares the external-identity-provider discovery endpoint.
/// </summary>
/// <remarks>
/// Auto-discovery is off, so this declaration is what routes the endpoint. Without it the class
/// compiles, the host starts, and a Login screen asking what it may offer gets a 404 — which reads
/// as "this host has no such feature" rather than "this declaration is missing".
/// </remarks>
[WebMcpTool("list_external_identity_providers", "List the external logins this host serves.")]
[TypeOption(typeof(AuthEndpoints), "ListExternalIdentityProviders")]
public class ListExternalIdentityProvidersOption : AuthEndpointBase<ListExternalIdentityProvidersEndpoint>
{
}
