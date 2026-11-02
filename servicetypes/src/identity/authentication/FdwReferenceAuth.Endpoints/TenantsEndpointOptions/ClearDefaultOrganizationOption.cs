using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The ClearDefaultOrganization endpoint: the caller lands in the company with no department at sign-in (FDW-2938).</summary>
[WebMcpTool("clear_default_organization", "Stop landing in a department when you sign in.")]
[TypeOption(typeof(TenantsEndpoints), "ClearDefaultOrganization")]
public class ClearDefaultOrganizationOption : TenantsEndpointBase<ClearDefaultOrganizationEndpoint>
{
}
