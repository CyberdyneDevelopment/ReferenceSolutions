using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.IdentityEndpointOptions;

/// <summary>Declares the QueryIdentities endpoint.</summary>
[WebMcpTool("query_identities", "Query managed identities by filter, order, page and field.", ReadOnly = true)]
[TypeOption(typeof(IdentityEndpoints), "QueryIdentities")]
public sealed class QueryIdentitiesOption : IdentityEndpointBase<QueryIdentitiesEndpoint>
{
}
