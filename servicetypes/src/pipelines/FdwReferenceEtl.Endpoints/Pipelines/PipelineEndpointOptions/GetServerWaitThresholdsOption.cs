using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

[WebMcpTool("get_server_wait_thresholds", "Read the thresholds on waiting runs the server states.", ReadOnly = true)]
[TypeOption(typeof(PipelineEndpoints), "GetServerWaitThresholds")]
public class GetServerWaitThresholdsOption : PipelineEndpointBase<GetServerWaitThresholdsEndpoint>
{
}
