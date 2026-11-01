using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint: list the settings of an organization with where each value comes from (FDW-1579).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListOrganizationInheritedSettingsEndpoint : ListOrganizationInheritedSettingsEndpointBase
{
    /// <inheritdoc />
    public ListOrganizationInheritedSettingsEndpoint(
        IInheritedSettings settings,
        ILogger<ListOrganizationInheritedSettingsEndpoint> logger)
        : base(logger, settings)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("organizations:read");
        Summary(s =>
        {
            s.Summary = "List the settings of an organization with where each value comes from";
            s.Description = "Lists every setting as it stands for an organization: its value, the level that set it and any lock. The caller must act in the organization.";
        });
        Tags("Settings");
    }
}
