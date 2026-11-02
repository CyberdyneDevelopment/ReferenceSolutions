using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The CreateOrganization endpoint (FDW-1758).</summary>
[WebMcpTool("create_organization", "Make a department with its starting roles and first administrator.")]
[TypeOption(typeof(TenantsEndpoints), "CreateOrganization")]
public class CreateOrganizationOption : TenantsEndpointBase<CreateOrganizationEndpoint>
{
}
