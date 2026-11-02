using Fdw.Services.Settings;
﻿using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Settings.Endpoints;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint to list all role-level setting overrides.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListRoleSettingsEndpoint : ListRoleSettingsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListRoleSettingsEndpoint"/> class.
    /// </summary>
    public ListRoleSettingsEndpoint(IRoleSettingConfigurationProvider provider, ILogger<ListRoleSettingsEndpoint> logger)
        : base(logger, provider) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Settings");
    }
}
