using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.InheritedSettingEndpointOptions;

/// <summary>The ListTenantInheritedSettings endpoint (FDW-1579).</summary>
[WebMcpTool("list_tenant_settings", "List the settings of a tenant with where each value comes from.")]
[TypeOption(typeof(InheritedSettingEndpoints), "ListTenantInheritedSettings")]
public class ListTenantInheritedSettingsEndpointOption : InheritedSettingEndpointBase<ListTenantInheritedSettingsEndpoint>
{
}
