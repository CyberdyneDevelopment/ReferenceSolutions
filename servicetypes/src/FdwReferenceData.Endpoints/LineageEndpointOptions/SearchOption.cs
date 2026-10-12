using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceData.Endpoints.LineageEndpointOptions;

/// <summary>The Search endpoint.</summary>
[WebMcpTool("search", "Search.", ReadOnly = true)]
[TypeOption(typeof(DataLineageEndpoints), "Search")]
public class SearchOption : DataLineageEndpointBase<SearchEndpoint>
{
}
