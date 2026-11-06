using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The PipelineEvent endpoint: the one door for pipeline events relayed from a host that serves no hub.</summary>
[TypeOption(typeof(ProxyEndpoints), "PipelineEvent")]
public class PipelineEventOption : ProxyEndpointBase<PipelineEventEndpoint>
{
}
