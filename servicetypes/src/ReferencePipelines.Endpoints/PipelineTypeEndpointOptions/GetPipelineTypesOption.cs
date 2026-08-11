using Fdw.Collections.Attributes;

namespace ReferencePipelines.Endpoints.PipelineTypeEndpointOptions;

/// <summary>The GetPipelineTypes endpoint.</summary>
[TypeOption(typeof(PipelineTypeEndpoints), "GetPipelineTypes")]
public class GetPipelineTypesOption : PipelineTypeEndpointBase<GetPipelineTypesEndpoint>
{
}
