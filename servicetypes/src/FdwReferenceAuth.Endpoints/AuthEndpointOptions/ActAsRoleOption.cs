using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The ActAsRole endpoint (FDW-2282).</summary>
[WebMcpTool("act_as_role", "Act with one role, holding only what it grants, until you return.")]
[TypeOption(typeof(AuthEndpoints), "ActAsRole")]
public class ActAsRoleOption : AuthEndpointBase<ActAsRoleEndpoint>
{
}
