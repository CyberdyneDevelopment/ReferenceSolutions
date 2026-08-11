using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceSettings.Endpoints.Logging;

namespace ReferenceSettings.Endpoints;

/// <summary>
/// Endpoint to create a new server-level setting.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateServerSettingEndpoint : CreateServerSettingEndpointBase
{
    private readonly ILogger<CreateServerSettingEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateServerSettingEndpoint"/> class.
    /// </summary>
    public CreateServerSettingEndpoint(
        SettingsConfigurationProvider provider,
        ILogger<CreateServerSettingEndpoint> logger)
        : base(provider)
    {
        _logger = logger ?? NullLogger<CreateServerSettingEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Settings");
    }

    /// <inheritdoc />
    protected override void OnBeforeCreate(string resourceName)
    {
        SettingsLog.CreatingServerSetting(_logger, resourceName);
    }

    /// <inheritdoc />
    protected override void OnAlreadyExists(string resourceName)
    {
        SettingsLog.ServerSettingAlreadyExists(_logger, resourceName);
    }

    /// <inheritdoc />
    protected override void OnAfterCreate(string resourceName)
    {
        SettingsLog.ServerSettingCreated(_logger, resourceName);
    }
}
