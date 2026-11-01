using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The QueryUsers endpoint.</summary>
[WebMcpTool("query_users", "Query users by filter, order, page and field", ReadOnly = true)]
[TypeOption(typeof(UserEndpoints), "QueryUsers")]
public class QueryUsersOption : UserEndpointBase<QueryUsersEndpoint>
{
}
