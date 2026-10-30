using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The SetDefaultOrganization endpoint: the department the caller lands in at sign-in (FDW-2938).</summary>
[WebMcpTool("set_default_organization", "Set the department you land in when you sign in.")]
[TypeOption(typeof(TenantsEndpoints), "SetDefaultOrganization")]
public class SetDefaultOrganizationOption : TenantsEndpointBase<SetDefaultOrganizationEndpoint>
{
}
