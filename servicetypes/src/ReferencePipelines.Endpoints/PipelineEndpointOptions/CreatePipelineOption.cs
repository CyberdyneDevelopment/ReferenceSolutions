using Fdw.Collections.Attributes;

namespace ReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The CreatePipeline endpoint.</summary>
[TypeOption(typeof(PipelineEndpoints), "CreatePipeline")]
public class CreatePipelineOption : PipelineEndpointBase<CreatePipelineEndpoint>
{
}
