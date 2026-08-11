using Fdw.Collections.Attributes;

namespace ReferenceEtlJobs.Endpoints.EtlJobsEndpointOptions;

/// <summary>The UnifiedTrigger endpoint.</summary>
[TypeOption(typeof(EtlJobsEndpoints), "UnifiedTrigger")]
public class UnifiedTriggerOption : EtlJobsEndpointBase<UnifiedTriggerEndpoint>
{
}
