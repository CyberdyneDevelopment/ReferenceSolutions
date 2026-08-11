using Fdw.Collections.Attributes;

namespace ReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The UpdatePipeline endpoint.</summary>
[TypeOption(typeof(PipelineEndpoints), "UpdatePipeline")]
public class UpdatePipelineOption : PipelineEndpointBase<UpdatePipelineEndpoint>
{
}
