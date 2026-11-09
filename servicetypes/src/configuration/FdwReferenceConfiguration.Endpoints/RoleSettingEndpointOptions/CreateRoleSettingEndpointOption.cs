using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.RoleSettingEndpointOptions;

/// <summary>The CreateRoleSetting endpoint.</summary>
[WebMcpTool("create_role_setting", "Create a role setting override.")]
[TypeOption(typeof(RoleSettingEndpoints), "CreateRoleSetting")]
public class CreateRoleSettingEndpointOption : RoleSettingEndpointBase<CreateRoleSettingEndpoint>
{
}
