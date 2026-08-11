using Fdw.Collections.Attributes;

namespace ReferenceEtlJobs.Endpoints.EtlJobsEndpointOptions;

/// <summary>The GetJobStatus endpoint.</summary>
[TypeOption(typeof(EtlJobsEndpoints), "GetJobStatus")]
public class GetJobStatusOption : EtlJobsEndpointBase<GetJobStatusEndpoint>
{
}
