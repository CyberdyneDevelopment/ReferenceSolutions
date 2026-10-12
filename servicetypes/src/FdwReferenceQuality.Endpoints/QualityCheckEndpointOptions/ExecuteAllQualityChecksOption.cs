using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceQuality.Endpoints.QualityCheckEndpointOptions;

/// <summary>The ExecuteAllQualityChecks endpoint.</summary>
[WebMcpTool("execute_all_quality_checks", "Execute all quality checks.")]
[TypeOption(typeof(QualityCheckEndpoints), "ExecuteAllQualityChecks")]
public class ExecuteAllQualityChecksOption : QualityCheckEndpointBase<ExecuteAllQualityChecksEndpoint>
{
}
