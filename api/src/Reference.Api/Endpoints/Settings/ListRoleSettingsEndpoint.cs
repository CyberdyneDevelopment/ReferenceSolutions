using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Abstractions;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Endpoints;

namespace Reference.Api.Endpoints.Settings;

/// <summary>
/// Endpoint to list all role-level setting overrides.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListRoleSettingsEndpoint : ListRoleSettingsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListRoleSettingsEndpoint"/> class.
    /// </summary>
    public ListRoleSettingsEndpoint(IServiceConfigurationProvider<RoleSettingConfiguration> provider)
        : base(provider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Settings");
    }
}
