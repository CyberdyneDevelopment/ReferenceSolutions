using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceData.Endpoints.LineageEndpointOptions;

/// <summary>The GetDataflowSources endpoint.</summary>
[WebMcpTool("get_dataflow_sources", "Get the sources and connections behind the dataflow graph.", ReadOnly = true)]
[TypeOption(typeof(DataLineageEndpoints), "GetDataflowSources")]
public class GetDataflowSourcesOption : DataLineageEndpointBase<GetDataflowSourcesEndpoint>
{
}
