using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints.Settings;

/// <summary>
/// Endpoint to create a tenant-level setting override.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateTenantSettingEndpoint : CreateTenantSettingEndpointBase
{
    private readonly ILogger<CreateTenantSettingEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateTenantSettingEndpoint"/> class.
    /// </summary>
    public CreateTenantSettingEndpoint(
        SettingsConfigurationProvider provider,
        ILogger<CreateTenantSettingEndpoint> logger)
        : base(provider)
    {
        _logger = logger ?? NullLogger<CreateTenantSettingEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Settings");
    }

    /// <inheritdoc />
    protected override void OnBeforeCreate(string resourceName)
    {
        SettingsLog.CreatingTenantSetting(_logger, resourceName);
    }

    /// <inheritdoc />
    protected override void OnAlreadyExists(string resourceName)
    {
        SettingsLog.TenantSettingAlreadyExists(_logger, resourceName);
    }

    /// <inheritdoc />
    protected override void OnAfterCreate(string resourceName)
    {
        SettingsLog.TenantSettingCreated(_logger, resourceName);
    }
}
