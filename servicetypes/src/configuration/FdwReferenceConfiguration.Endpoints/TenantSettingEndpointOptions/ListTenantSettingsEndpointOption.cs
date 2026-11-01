using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.TenantSettingEndpointOptions;

/// <summary>The ListTenantSettings endpoint.</summary>
[WebMcpTool("list_tenant_settings", "List tenant setting overrides.", ReadOnly = true)]
[TypeOption(typeof(TenantSettingEndpoints), "ListTenantSettings")]
public class ListTenantSettingsEndpointOption : TenantSettingEndpointBase<ListTenantSettingsEndpoint>
{
}
