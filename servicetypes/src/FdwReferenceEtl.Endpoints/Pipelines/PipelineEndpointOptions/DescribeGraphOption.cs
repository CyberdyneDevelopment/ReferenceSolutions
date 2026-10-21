using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The DescribeGraph endpoint.</summary>
[WebMcpTool("describe_graph_pipeline", "Describe a graph pipeline draft.", ReadOnly = true)]
[TypeOption(typeof(PipelineEndpoints), "DescribeGraph")]
public class DescribeGraphOption : PipelineEndpointBase<DescribeGraphEndpoint>
{
}
