using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.InheritedSettingEndpointOptions;

/// <summary>The SetPlatformInheritedSetting endpoint (FDW-1579).</summary>
[WebMcpTool("set_platform_setting", "Set a platform setting and lock it against every tenant.")]
[TypeOption(typeof(InheritedSettingEndpoints), "SetPlatformInheritedSetting")]
public class SetPlatformInheritedSettingEndpointOption : InheritedSettingEndpointBase<SetPlatformInheritedSettingEndpoint>
{
}
