using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint: set a tenant value for a setting and lock it against its organizations (FDW-1579).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SetTenantInheritedSettingEndpoint : SetTenantInheritedSettingEndpointBase
{
    /// <inheritdoc />
    public SetTenantInheritedSettingEndpoint(
        IInheritedSettings settings,
        IAdministrativeAudit audit,
        ISystemRoleConfiguration systemRoles,
        ILogger<SetTenantInheritedSettingEndpoint> logger)
        : base(logger, settings, audit, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("tenants:write");
        Summary(s =>
        {
            s.Summary = "Set a tenant value for a setting and lock it against its organizations";
            s.Description = "Sets the tenant value of a setting and locks it against the tenant organizations, or leaves it open. Refused while the platform has the setting locked.";
        });
        Tags("Settings");
    }
}
