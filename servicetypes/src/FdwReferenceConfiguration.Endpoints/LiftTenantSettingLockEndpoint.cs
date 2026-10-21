using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint: lift the lock a tenant holds on a setting (FDW-1579).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class LiftTenantSettingLockEndpoint : LiftTenantSettingLockEndpointBase
{
    /// <inheritdoc />
    public LiftTenantSettingLockEndpoint(
        IInheritedSettings settings,
        IAdministrativeAudit audit,
        ISystemRoleConfiguration systemRoles,
        ILogger<LiftTenantSettingLockEndpoint> logger)
        : base(logger, settings, audit, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("tenants:write");
        Summary(s =>
        {
            s.Summary = "Lift the lock a tenant holds on a setting";
            s.Description = "Lifts the lock a tenant holds on a setting and keeps its value. Platform administrators only.";
        });
        Tags("Settings");
    }
}
