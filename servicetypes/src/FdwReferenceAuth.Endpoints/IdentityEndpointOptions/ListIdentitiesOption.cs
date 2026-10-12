using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.IdentityEndpointOptions;

/// <summary>Declares the ListIdentities endpoint.</summary>
[WebMcpTool("list_identities", "List identities.", ReadOnly = true)]
[TypeOption(typeof(IdentityEndpoints), "ListIdentities")]
public sealed class ListIdentitiesOption : IdentityEndpointBase<ListIdentitiesEndpoint>
{
}
