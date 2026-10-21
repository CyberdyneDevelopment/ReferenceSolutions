using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.InheritedSettingEndpointOptions;

/// <summary>The ListOrganizationInheritedSettings endpoint (FDW-1579).</summary>
[WebMcpTool("list_organization_settings", "List the settings of an organization with where each value comes from.")]
[TypeOption(typeof(InheritedSettingEndpoints), "ListOrganizationInheritedSettings")]
public class ListOrganizationInheritedSettingsEndpointOption : InheritedSettingEndpointBase<ListOrganizationInheritedSettingsEndpoint>
{
}
