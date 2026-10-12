using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePromotion.Endpoints.EnvironmentEndpointOptions;

/// <summary>The ListEnvironments endpoint.</summary>
[WebMcpTool("list_environments", "List environments.", ReadOnly = true)]
[TypeOption(typeof(EnvironmentEndpoints), "ListEnvironments")]
public class ListEnvironmentsOption : EnvironmentEndpointBase<ListEnvironmentsEndpoint>
{
}
