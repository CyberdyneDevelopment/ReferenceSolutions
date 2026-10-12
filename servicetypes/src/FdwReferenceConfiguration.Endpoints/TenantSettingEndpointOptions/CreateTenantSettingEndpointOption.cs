using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.TenantSettingEndpointOptions;

/// <summary>The CreateTenantSetting endpoint.</summary>
[WebMcpTool("create_tenant_setting", "Create a tenant setting override.")]
[TypeOption(typeof(TenantSettingEndpoints), "CreateTenantSetting")]
public class CreateTenantSettingEndpointOption : TenantSettingEndpointBase<CreateTenantSettingEndpoint>
{
}
