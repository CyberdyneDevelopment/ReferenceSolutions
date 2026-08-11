using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceSettings.Endpoints.Logging;

namespace ReferenceSettings.Endpoints;

/// <summary>
/// Endpoint to update a tenant-level setting override.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateTenantSettingEndpoint : UpdateTenantSettingEndpointBase
{
    private readonly ILogger<UpdateTenantSettingEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTenantSettingEndpoint"/> class.
    /// </summary>
    public UpdateTenantSettingEndpoint(
        SettingsConfigurationProvider provider,
        ILogger<UpdateTenantSettingEndpoint> logger)
        : base(provider)
    {
        _logger = logger ?? NullLogger<UpdateTenantSettingEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Settings");
    }

    /// <inheritdoc />
    protected override void OnBeforeUpdate(string identifier)
    {
        SettingsLog.UpdatingTenantSetting(_logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnNotFound(string identifier)
    {
        SettingsLog.TenantSettingNotFoundForUpdate(_logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnAfterUpdate(string identifier)
    {
        SettingsLog.TenantSettingUpdated(_logger, identifier);
    }
}
