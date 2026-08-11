using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceSettings.Endpoints.Logging;

namespace ReferenceSettings.Endpoints;

/// <summary>
/// Endpoint to update an existing server-level setting.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateServerSettingEndpoint : UpdateServerSettingEndpointBase
{
    private readonly ILogger<UpdateServerSettingEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateServerSettingEndpoint"/> class.
    /// </summary>
    public UpdateServerSettingEndpoint(
        SettingsConfigurationProvider provider,
        ILogger<UpdateServerSettingEndpoint> logger)
        : base(provider)
    {
        _logger = logger ?? NullLogger<UpdateServerSettingEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Settings");
    }

    /// <inheritdoc />
    protected override void OnBeforeUpdate(string identifier)
    {
        SettingsLog.UpdatingServerSetting(_logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnNotFound(string identifier)
    {
        SettingsLog.ServerSettingNotFoundForUpdate(_logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnAfterUpdate(string identifier)
    {
        SettingsLog.ServerSettingUpdated(_logger, identifier);
    }
}
