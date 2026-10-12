using Fdw.Services.Settings;
﻿using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Settings.Endpoints;

namespace FdwReferenceSettings.Endpoints;

/// <summary>
/// Endpoint to list all server-level settings.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListServerSettingsEndpoint : ListServerSettingsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListServerSettingsEndpoint"/> class.
    /// </summary>
    public ListServerSettingsEndpoint(IServerSettingConfigurationProvider provider, ILogger<ListServerSettingsEndpoint> logger)
        : base(logger, provider) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Settings");
    }
}
