using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.RoleSettingEndpointOptions;

/// <summary>The ListRoleSettings endpoint.</summary>
[WebMcpTool("list_role_settings", "List role setting overrides.", ReadOnly = true)]
[TypeOption(typeof(RoleSettingEndpoints), "ListRoleSettings")]
public class ListRoleSettingsEndpointOption : RoleSettingEndpointBase<ListRoleSettingsEndpoint>
{
}
