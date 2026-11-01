using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceQuality.Endpoints.QualityCheckEndpointOptions;

/// <summary>The ExecuteQualityCheck endpoint.</summary>
[WebMcpTool("execute_quality_check", "Execute quality check.")]
[TypeOption(typeof(QualityCheckEndpoints), "ExecuteQualityCheck")]
public class ExecuteQualityCheckOption : QualityCheckEndpointBase<ExecuteQualityCheckEndpoint>
{
}
