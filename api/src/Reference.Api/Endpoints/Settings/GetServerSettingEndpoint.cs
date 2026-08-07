using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints.Settings;

/// <summary>
/// Endpoint to get a server-level setting by name.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetServerSettingEndpoint : GetServerSettingEndpointBase
{
    private readonly ILogger<GetServerSettingEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetServerSettingEndpoint"/> class.
    /// </summary>
    public GetServerSettingEndpoint(
        SettingsConfigurationProvider provider,
        ILogger<GetServerSettingEndpoint> logger)
        : base(provider)
    {
        _logger = logger ?? NullLogger<GetServerSettingEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Settings");
    }

    /// <inheritdoc />
    protected override void OnBeforeGet(string identifier)
    {
        SettingsLog.GettingServerSetting(_logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnNotFound(string identifier)
    {
        SettingsLog.ServerSettingNotFound(_logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnAfterGet(string identifier)
    {
        SettingsLog.ServerSettingRetrieved(_logger, identifier);
    }
}
