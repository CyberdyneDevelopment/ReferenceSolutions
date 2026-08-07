using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Abstractions;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Endpoints;

namespace Reference.Api.Endpoints.Settings;

/// <summary>
/// Endpoint to list all tenant-level setting overrides.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListTenantSettingsEndpoint : ListTenantSettingsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListTenantSettingsEndpoint"/> class.
    /// </summary>
    public ListTenantSettingsEndpoint(IServiceConfigurationProvider<TenantSettingConfiguration> provider)
        : base(provider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Settings");
    }
}
