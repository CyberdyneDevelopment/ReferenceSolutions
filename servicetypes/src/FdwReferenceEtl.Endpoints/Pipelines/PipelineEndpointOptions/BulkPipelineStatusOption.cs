using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The BulkPipelineStatus endpoint.</summary>
[WebMcpTool("bulk_pipeline_status", "Bulk pipeline status.")]
[TypeOption(typeof(PipelineEndpoints), "BulkPipelineStatus")]
public class BulkPipelineStatusOption : PipelineEndpointBase<BulkPipelineStatusEndpoint>
{
}
