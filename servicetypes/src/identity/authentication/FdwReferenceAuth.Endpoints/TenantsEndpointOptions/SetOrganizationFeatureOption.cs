using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The SetOrganizationFeature endpoint (FDW-1580).</summary>
[WebMcpTool("set_organization_feature", "Switch a feature on or off for an organization.")]
[TypeOption(typeof(TenantsEndpoints), "SetOrganizationFeature")]
public class SetOrganizationFeatureOption : TenantsEndpointBase<SetOrganizationFeatureEndpoint>
{
}
