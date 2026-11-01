using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtlJobs.Endpoints.EtlJobsEndpointOptions;

/// <summary>The GetJobStatus endpoint.</summary>
[WebMcpTool("get_job_status", "[DEPRECATED] Get ETL job status", ReadOnly = true)]
[TypeOption(typeof(EtlJobsEndpoints), "GetJobStatus")]
public class GetJobStatusOption : EtlJobsEndpointBase<GetJobStatusEndpoint>
{
}
