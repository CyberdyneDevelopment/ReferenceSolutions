using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.RoleSettingEndpointOptions;

/// <summary>The UpdateRoleSetting endpoint.</summary>
[WebMcpTool("update_role_setting", "Update a role setting override.")]
[TypeOption(typeof(RoleSettingEndpoints), "UpdateRoleSetting")]
public class UpdateRoleSettingEndpointOption : RoleSettingEndpointBase<UpdateRoleSettingEndpoint>
{
}
