using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineTypeEndpointOptions;

/// <summary>The GetPipelineTypes endpoint.</summary>
[WebMcpTool("get_pipeline_types", "Get pipeline types.", ReadOnly = true)]
[TypeOption(typeof(PipelineTypeEndpoints), "GetPipelineTypes")]
public class GetPipelineTypesOption : PipelineTypeEndpointBase<GetPipelineTypesEndpoint>
{
}
