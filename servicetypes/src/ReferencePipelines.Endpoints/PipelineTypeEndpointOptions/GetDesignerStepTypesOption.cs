using Fdw.Collections.Attributes;

namespace ReferencePipelines.Endpoints.PipelineTypeEndpointOptions;

/// <summary>The GetDesignerStepTypes endpoint.</summary>
[TypeOption(typeof(PipelineTypeEndpoints), "GetDesignerStepTypes")]
public class GetDesignerStepTypesOption : PipelineTypeEndpointBase<GetDesignerStepTypesEndpoint>
{
}
