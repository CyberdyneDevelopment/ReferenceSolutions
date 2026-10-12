using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The ValidateExistingPipeline endpoint.</summary>
[WebMcpTool("validate_existing_pipeline", "Check an edit of a saved pipeline without saving it.", ReadOnly = true)]
[TypeOption(typeof(PipelineEndpoints), "ValidateExistingPipeline")]
public class ValidateExistingPipelineOption : PipelineEndpointBase<ValidateExistingPipelineEndpoint>
{
}
