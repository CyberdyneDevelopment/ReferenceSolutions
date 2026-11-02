using Fdw.Services.Settings;
﻿using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Settings.Endpoints;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint to list all tenant-level setting overrides.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListTenantSettingsEndpoint : ListTenantSettingsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListTenantSettingsEndpoint"/> class.
    /// </summary>
    public ListTenantSettingsEndpoint(ITenantSettingConfigurationProvider provider, ILogger<ListTenantSettingsEndpoint> logger)
        : base(logger, provider) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Settings");
    }
}
