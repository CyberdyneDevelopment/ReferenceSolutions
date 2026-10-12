using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceData.Endpoints.LineageEndpointOptions;

/// <summary>The Find endpoint.</summary>
// Why no closure class: FindEndpointBase is concrete and complete — it needs
// declaring, not specialising. Deriving an empty closure would add a second type for nothing.
[WebMcpTool("find", "Find.")]
[TypeOption(typeof(DataLineageEndpoints), "Find")]
public class FindOption : DataLineageEndpointBase<Fdw.Web.Search.Endpoints.FindEndpoint>
{
}
