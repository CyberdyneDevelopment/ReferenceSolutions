using Fdw.Collections.Attributes;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The ResumeEtlExecutionProxy endpoint.</summary>
[TypeOption(typeof(ProxyEndpoints), "ResumeEtlExecutionProxy")]
public class ResumeEtlExecutionProxyOption : ProxyEndpointBase<ResumeEtlExecutionProxyEndpoint>
{
}
