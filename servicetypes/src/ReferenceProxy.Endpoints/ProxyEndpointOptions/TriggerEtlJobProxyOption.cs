using Fdw.Collections.Attributes;

namespace ReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The TriggerEtlJobProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "TriggerEtlJobProxy")]
public class TriggerEtlJobProxyOption : ProxyEndpointBase<TriggerEtlJobProxyEndpoint>
{
}
