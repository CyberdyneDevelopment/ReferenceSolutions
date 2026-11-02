using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint: list the settings of a tenant with where each value comes from (FDW-1579).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListTenantInheritedSettingsEndpoint : ListTenantInheritedSettingsEndpointBase
{
    /// <inheritdoc />
    public ListTenantInheritedSettingsEndpoint(
        IInheritedSettings settings,
        ISystemRoleConfiguration systemRoles,
        ILogger<ListTenantInheritedSettingsEndpoint> logger)
        : base(logger, settings, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("tenants:read");
        Summary(s =>
        {
            s.Summary = "List the settings of a tenant with where each value comes from";
            s.Description = "Lists every setting as it stands for a tenant: its value, the level that set it and any lock. A platform administrator reads any tenant, anyone else their own.";
        });
        Tags("Settings");
    }
}
