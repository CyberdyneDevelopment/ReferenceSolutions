using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceSettings.Endpoints.Logging;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint to update a role-level setting override.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateRoleSettingEndpoint : UpdateRoleSettingEndpointBase
{
    private readonly ILogger<UpdateRoleSettingEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRoleSettingEndpoint"/> class.
    /// </summary>
    public UpdateRoleSettingEndpoint(IRoleSettingConfigurationProvider provider, ILogger<UpdateRoleSettingEndpoint> logger)
        : base(logger, provider) {
        _logger = logger ?? NullLogger<UpdateRoleSettingEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Settings");
    }

    /// <inheritdoc />
    protected override void OnBeforeUpdate(string identifier)
    {
        SettingsLog.UpdatingRoleSetting(_logger, identifier, string.Empty);
    }

    /// <inheritdoc />
    protected override void OnNotFound(string identifier)
    {
        SettingsLog.RoleSettingNotFoundForUpdate(_logger, identifier, string.Empty);
    }

    /// <inheritdoc />
    protected override void OnAfterUpdate(string identifier)
    {
        SettingsLog.RoleSettingUpdated(_logger, identifier, string.Empty);
    }
}
