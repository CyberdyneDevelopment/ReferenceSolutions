using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtlJobs.Endpoints.EtlJobsEndpointOptions;

/// <summary>The UnifiedTrigger endpoint.</summary>
[WebMcpTool("unified_trigger", "Unified ETL trigger")]
[TypeOption(typeof(EtlJobsEndpoints), "UnifiedTrigger")]
public class UnifiedTriggerOption : EtlJobsEndpointBase<UnifiedTriggerEndpoint>
{
}
