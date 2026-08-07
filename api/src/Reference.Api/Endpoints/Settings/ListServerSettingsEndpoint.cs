using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Abstractions;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Endpoints;

namespace Reference.Api.Endpoints.Settings;

/// <summary>
/// Endpoint to list all server-level settings.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListServerSettingsEndpoint : ListServerSettingsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListServerSettingsEndpoint"/> class.
    /// </summary>
    public ListServerSettingsEndpoint(IServiceConfigurationProvider<ServerSettingConfiguration> provider)
        : base(provider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Settings");
    }
}
