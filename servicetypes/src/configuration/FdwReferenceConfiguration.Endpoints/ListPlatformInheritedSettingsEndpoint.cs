using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint: list the platform settings with their locks (FDW-1579).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListPlatformInheritedSettingsEndpoint : ListPlatformInheritedSettingsEndpointBase
{
    /// <inheritdoc />
    public ListPlatformInheritedSettingsEndpoint(
        IInheritedSettings settings,
        ISystemRoleConfiguration systemRoles,
        ILogger<ListPlatformInheritedSettingsEndpoint> logger)
        : base(logger, settings, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("tenants:read");
        Summary(s =>
        {
            s.Summary = "List the platform settings with their locks";
            s.Description = "Lists every setting the platform defines, with its value and whether it is locked. Platform administrators only.";
        });
        Tags("Settings");
    }
}
