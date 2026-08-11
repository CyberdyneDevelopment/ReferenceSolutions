using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Fdw.UI.Themes;
using Fdw.UI.Themes.Clients.Models;
using Fdw.UI.Themes.Configuration;
using Fdw.UI.Themes.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceShared.Endpoints.Logging;

namespace ReferenceShared.Endpoints;

/// <summary>
/// Gets the default theme from database-backed configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetDefaultThemeEndpoint : GetDefaultThemeEndpoint<ThemeConfiguration>
{
    private readonly ILogger<GetDefaultThemeEndpoint> _logger;
    private readonly ThemeConfigurationProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetDefaultThemeEndpoint"/> class.
    /// </summary>
    public GetDefaultThemeEndpoint(
        ILogger<GetDefaultThemeEndpoint> logger,
        ThemeConfigurationProvider provider)
    {
        _logger = logger ?? NullLogger<GetDefaultThemeEndpoint>.Instance;
        _provider = provider;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/themes/default");
        Policies("configurations:read");
        Summary(s =>
        {
            s.Summary = "Get default theme";
            s.Description = "Returns the current default theme configuration.";
        });
    }

    protected override ThemeConfiguration LoadDefaultTheme()
    {
        ThemeLog.GettingDefaultTheme(_logger);

        // Why: Provider.GetAll is async; base class contract is sync. Thread-pool thread, no SyncContext.
        var allThemesResult = _provider.Get(CancellationToken.None).GetAwaiter().GetResult();
        var allThemes = allThemesResult.Value ?? [];

        var managed = allThemes.FirstOrDefault(t => t.IsDefault);

        if (managed == null)
        {
            // Fall back to first theme named "fractal", or create a default
            managed = allThemes
                .FirstOrDefault(t => string.Equals(t.Name, "fractal", StringComparison.OrdinalIgnoreCase));
        }

        if (managed != null)
        {
            return managed.ToDto();
        }

        // Ultimate fallback if no themes in database
        return ThemeConfiguration.CreateFractalTheme();
    }
}
