using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.IdentityEndpointOptions;

/// <summary>Declares the ListIdentityMechanisms endpoint.</summary>
[WebMcpTool("list_identity_mechanisms", "List identity mechanisms.", ReadOnly = true)]
[TypeOption(typeof(IdentityEndpoints), "ListIdentityMechanisms")]
public sealed class ListIdentityMechanismsOption : IdentityEndpointBase<ListIdentityMechanismsEndpoint>
{
}
