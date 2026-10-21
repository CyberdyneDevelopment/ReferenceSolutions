using Fdw.Collections.Attributes;

namespace FdwReferenceEtlJobs.Endpoints.EtlJobsEndpointOptions;

/// <summary>The WebhookTrigger endpoint: the address an outside system calls to start a pipeline (FDW-1884).</summary>
[TypeOption(typeof(EtlJobsEndpoints), "WebhookTrigger")]
public class WebhookTriggerOption : EtlJobsEndpointBase<WebhookTriggerEndpoint>
{
}
