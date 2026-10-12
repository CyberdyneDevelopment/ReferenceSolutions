using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The ValidateExistingPipelineProxy endpoint.</summary>
[WebMcpTool("validate_existing_pipeline_proxy", "Check an edit of a saved pipeline without saving it (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "ValidateExistingPipelineProxy")]
public class ValidateExistingPipelineProxyOption : ProxyEndpointBase<ValidateExistingPipelineProxyEndpoint>
{
}
