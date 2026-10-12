using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.IdentityEndpointOptions;

/// <summary>Declares the VerifyIdentity endpoint.</summary>
[WebMcpTool("verify_identity", "Verify identity.")]
[TypeOption(typeof(IdentityEndpoints), "VerifyIdentity")]
public sealed class VerifyIdentityOption : IdentityEndpointBase<VerifyIdentityEndpoint>
{
}
