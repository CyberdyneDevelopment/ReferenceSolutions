using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The ValidatePipeline endpoint.</summary>
[WebMcpTool("validate_pipeline", "Check a pipeline draft without saving it.", ReadOnly = true)]
[TypeOption(typeof(PipelineEndpoints), "ValidatePipeline")]
public class ValidatePipelineOption : PipelineEndpointBase<ValidatePipelineEndpoint>
{
}
