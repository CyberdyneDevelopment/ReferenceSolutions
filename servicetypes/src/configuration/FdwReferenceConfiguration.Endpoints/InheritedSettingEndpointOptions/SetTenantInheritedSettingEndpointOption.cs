using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.InheritedSettingEndpointOptions;

/// <summary>The SetTenantInheritedSetting endpoint (FDW-1579).</summary>
[WebMcpTool("set_tenant_setting", "Set a tenant value for a setting and lock it against its organizations.")]
[TypeOption(typeof(InheritedSettingEndpoints), "SetTenantInheritedSetting")]
public class SetTenantInheritedSettingEndpointOption : InheritedSettingEndpointBase<SetTenantInheritedSettingEndpoint>
{
}
