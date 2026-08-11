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
/// Gets a specific theme by name from database-backed configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetThemeEndpoint : GetThemeEndpoint<ThemeConfiguration>
{
    private readonly ILogger<GetThemeEndpoint> _logger;
    private readonly ThemeConfigurationProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetThemeEndpoint"/> class.
    /// </summary>
    public GetThemeEndpoint(
        ILogger<GetThemeEndpoint> logger,
        ThemeConfigurationProvider provider)
    {
        _logger = logger ?? NullLogger<GetThemeEndpoint>.Instance;
        _provider = provider;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/themes/{Name}");
        Policies("configurations:read");
        Summary(s =>
        {
            s.Summary = "Get theme by name";
            s.Description = "Returns the full configuration for a specific theme.";
        });
    }

    protected override ThemeConfiguration? FindTheme(string name)
    {
        // Why: Provider.Get is async; base class contract is sync. Thread-pool thread, no SyncContext.
        var result = _provider.Get(name, CancellationToken.None).GetAwaiter().GetResult();

        if (!result.IsSuccess || result.Value == null)
        {
            ThemeLog.ThemeNotFound(_logger, name);
            return null;
        }

        ThemeLog.GettingTheme(_logger, result.Value.Name);
        return result.Value.ToDto();
    }
}
