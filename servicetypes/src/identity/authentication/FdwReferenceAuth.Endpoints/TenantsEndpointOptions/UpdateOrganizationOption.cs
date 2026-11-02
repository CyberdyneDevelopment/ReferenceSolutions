using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The UpdateOrganization endpoint (FDW-1758).</summary>
[WebMcpTool("update_organization", "Rename, disable or enable a department.")]
[TypeOption(typeof(TenantsEndpoints), "UpdateOrganization")]
public class UpdateOrganizationOption : TenantsEndpointBase<UpdateOrganizationEndpoint>
{
}
