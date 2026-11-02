using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The BindExternalIdentity endpoint.</summary>
[WebMcpTool("bind_external_identity", "Bind an external identity to an account.")]
[TypeOption(typeof(UserEndpoints), "BindExternalIdentity")]
public class BindExternalIdentityOption : UserEndpointBase<BindExternalIdentityEndpoint>
{
}
