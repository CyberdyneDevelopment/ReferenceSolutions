using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceSettings.Endpoints.Logging;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint to create a role-level setting override.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateRoleSettingEndpoint : CreateRoleSettingEndpointBase
{
    private readonly ILogger<CreateRoleSettingEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateRoleSettingEndpoint"/> class.
    /// </summary>
    public CreateRoleSettingEndpoint(IRoleSettingConfigurationProvider provider, ILogger<CreateRoleSettingEndpoint> logger)
        : base(logger, provider) {
        _logger = logger ?? NullLogger<CreateRoleSettingEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Settings");
    }

    /// <inheritdoc />
    protected override void OnBeforeCreate(string resourceName)
    {
        SettingsLog.CreatingRoleSetting(_logger, resourceName, string.Empty);
    }

    /// <inheritdoc />
    protected override void OnAlreadyExists(string resourceName)
    {
        SettingsLog.RoleSettingAlreadyExists(_logger, resourceName, string.Empty);
    }

    /// <inheritdoc />
    protected override void OnAfterCreate(string resourceName)
    {
        SettingsLog.RoleSettingCreated(_logger, resourceName, string.Empty);
    }
}
