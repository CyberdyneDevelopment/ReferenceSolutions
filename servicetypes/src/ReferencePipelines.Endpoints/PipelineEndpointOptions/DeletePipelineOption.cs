using Fdw.Collections.Attributes;

namespace ReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The DeletePipeline endpoint.</summary>
[TypeOption(typeof(PipelineEndpoints), "DeletePipeline")]
public class DeletePipelineOption : PipelineEndpointBase<DeletePipelineEndpoint>
{
}
