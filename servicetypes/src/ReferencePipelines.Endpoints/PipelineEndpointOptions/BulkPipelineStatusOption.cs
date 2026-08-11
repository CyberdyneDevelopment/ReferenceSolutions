using Fdw.Collections.Attributes;

namespace ReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The BulkPipelineStatus endpoint.</summary>
[TypeOption(typeof(PipelineEndpoints), "BulkPipelineStatus")]
public class BulkPipelineStatusOption : PipelineEndpointBase<BulkPipelineStatusEndpoint>
{
}
