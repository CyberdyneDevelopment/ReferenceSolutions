using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The ValidatePipelineProxy endpoint.</summary>
[WebMcpTool("validate_pipeline_proxy", "Check a pipeline draft without saving it (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "ValidatePipelineProxy")]
public class ValidatePipelineProxyOption : ProxyEndpointBase<ValidatePipelineProxyEndpoint>
{
}
