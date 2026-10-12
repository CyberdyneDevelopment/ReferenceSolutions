using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The UnbindExternalIdentity endpoint.</summary>
[WebMcpTool("unbind_external_identity", "Remove an external-identity binding.")]
[TypeOption(typeof(UserEndpoints), "UnbindExternalIdentity")]
public class UnbindExternalIdentityOption : UserEndpointBase<UnbindExternalIdentityEndpoint>
{
}
