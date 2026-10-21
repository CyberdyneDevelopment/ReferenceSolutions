using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint: set an organization value for a setting (FDW-1579).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SetOrganizationInheritedSettingEndpoint : SetOrganizationInheritedSettingEndpointBase
{
    /// <inheritdoc />
    public SetOrganizationInheritedSettingEndpoint(
        IInheritedSettings settings,
        IAdministrativeAudit audit,
        ILogger<SetOrganizationInheritedSettingEndpoint> logger)
        : base(logger, settings, audit)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("organizations:write");
        Summary(s =>
        {
            s.Summary = "Set an organization value for a setting";
            s.Description = "Sets the organization value of a setting. Refused while its tenant or the platform has the setting locked. The caller must act in the organization.";
        });
        Tags("Settings");
    }
}
