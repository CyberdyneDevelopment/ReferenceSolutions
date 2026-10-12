using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.TenantSettingEndpointOptions;

/// <summary>The UpdateTenantSetting endpoint.</summary>
[WebMcpTool("update_tenant_setting", "Update a tenant setting override.")]
[TypeOption(typeof(TenantSettingEndpoints), "UpdateTenantSetting")]
public class UpdateTenantSettingEndpointOption : TenantSettingEndpointBase<UpdateTenantSettingEndpoint>
{
}
