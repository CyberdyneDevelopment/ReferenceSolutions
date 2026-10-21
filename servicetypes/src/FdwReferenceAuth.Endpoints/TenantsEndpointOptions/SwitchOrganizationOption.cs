using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The SwitchOrganization endpoint: re-issues the caller's token for a department of the company they act in (FDW-1802).</summary>
[WebMcpTool("switch_organization", "Act in another department; returns a new access token for it.")]
[TypeOption(typeof(TenantsEndpoints), "SwitchOrganization")]
public class SwitchOrganizationOption : TenantsEndpointBase<SwitchOrganizationEndpoint>
{
}
