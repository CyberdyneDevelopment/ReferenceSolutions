using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.InheritedSettingEndpointOptions;

/// <summary>The SetOrganizationInheritedSetting endpoint (FDW-1579).</summary>
[WebMcpTool("set_organization_setting", "Set an organization value for a setting.")]
[TypeOption(typeof(InheritedSettingEndpoints), "SetOrganizationInheritedSetting")]
public class SetOrganizationInheritedSettingEndpointOption : InheritedSettingEndpointBase<SetOrganizationInheritedSettingEndpoint>
{
}
