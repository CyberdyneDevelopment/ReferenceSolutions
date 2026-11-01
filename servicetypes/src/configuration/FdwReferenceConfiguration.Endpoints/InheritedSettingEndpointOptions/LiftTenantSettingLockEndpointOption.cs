using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.InheritedSettingEndpointOptions;

/// <summary>The LiftTenantSettingLock endpoint (FDW-1579).</summary>
[WebMcpTool("lift_tenant_setting_lock", "Lift the lock a tenant holds on a setting.")]
[TypeOption(typeof(InheritedSettingEndpoints), "LiftTenantSettingLock")]
public class LiftTenantSettingLockEndpointOption : InheritedSettingEndpointBase<LiftTenantSettingLockEndpoint>
{
}
