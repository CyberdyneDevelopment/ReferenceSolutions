using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint: set a platform setting and lock it against every tenant (FDW-1579).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SetPlatformInheritedSettingEndpoint : SetPlatformInheritedSettingEndpointBase
{
    /// <inheritdoc />
    public SetPlatformInheritedSettingEndpoint(
        IInheritedSettings settings,
        IAdministrativeAudit audit,
        ISystemRoleConfiguration systemRoles,
        ILogger<SetPlatformInheritedSettingEndpoint> logger)
        : base(logger, settings, audit, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("tenants:write");
        Summary(s =>
        {
            s.Summary = "Set a platform setting and lock it against every tenant";
            s.Description = "Sets the platform value of a setting and locks it against every tenant, or leaves it open. Setting it without the lock lifts the platform lock. Platform administrators only.";
        });
        Tags("Settings");
    }
}
