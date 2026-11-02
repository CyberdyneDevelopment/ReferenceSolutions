using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The PreviewStandIn endpoint.</summary>
[WebMcpTool("preview_stand_in", "Preview a stand-in strategy's generated values.", ReadOnly = true)]
[TypeOption(typeof(DataSetEndpoints), "PreviewStandIn")]
public class PreviewStandInOption : DataSetEndpointBase<PreviewStandInEndpoint>
{
}
