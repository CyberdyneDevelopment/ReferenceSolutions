using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Abstractions;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Endpoints;

namespace ReferenceSettings.Endpoints;

/// <summary>
/// Endpoint to list all server-level settings.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListServerSettingsEndpoint : ListServerSettingsEndpointBase
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
