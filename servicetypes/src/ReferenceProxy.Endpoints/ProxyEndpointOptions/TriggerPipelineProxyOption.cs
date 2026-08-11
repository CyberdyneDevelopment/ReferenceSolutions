using Fdw.Collections.Attributes;

namespace ReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The TriggerPipelineProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "TriggerPipelineProxy")]
public class TriggerPipelineProxyOption : ProxyEndpointBase<TriggerPipelineProxyEndpoint>
{
}
