using Fdw.Collections.Attributes;

namespace ReferenceEtlJobs.Endpoints.EtlJobsEndpointOptions;

/// <summary>The TriggerJob endpoint.</summary>
[TypeOption(typeof(EtlJobsEndpoints), "TriggerJob")]
public class TriggerJobOption : EtlJobsEndpointBase<TriggerJobEndpoint>
{
}
