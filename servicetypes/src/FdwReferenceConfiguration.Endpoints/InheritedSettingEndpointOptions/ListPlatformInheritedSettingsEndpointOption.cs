using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.InheritedSettingEndpointOptions;

/// <summary>The ListPlatformInheritedSettings endpoint (FDW-1579).</summary>
[WebMcpTool("list_platform_settings", "List the platform settings with their locks.")]
[TypeOption(typeof(InheritedSettingEndpoints), "ListPlatformInheritedSettings")]
public class ListPlatformInheritedSettingsEndpointOption : InheritedSettingEndpointBase<ListPlatformInheritedSettingsEndpoint>
{
}
