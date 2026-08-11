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
/// Sets a theme as the default in database-backed configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public class SetDefaultThemeEndpoint : Fdw.UI.Themes.Endpoints.SetDefaultThemeEndpoint
{
    private readonly ILogger<SetDefaultThemeEndpoint> _logger;
    private readonly ThemeConfigurationProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetDefaultThemeEndpoint"/> class.
    /// </summary>
    public SetDefaultThemeEndpoint(
        ILogger<SetDefaultThemeEndpoint> logger,
        ThemeConfigurationProvider provider)
    {
        _logger = logger ?? NullLogger<SetDefaultThemeEndpoint>.Instance;
        _provider = provider;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/themes/{Name}/default");
        Policies("configurations:write");
        Summary(s =>
        {
            s.Summary = "Set default theme";
            s.Description = "Sets a theme as the system default.";
        });
    }

    // Why: Provider.Get is async; base class contract is sync. Thread-pool thread, no SyncContext.
    protected override bool ThemeExists(string name)
    {
        var result = _provider.Get(name, CancellationToken.None).GetAwaiter().GetResult();
        return result.IsSuccess && result.Value is not null;
    }

    protected override void ApplyDefault(string name)
    {
        ThemeLog.SettingDefaultTheme(_logger, name);

        // Why: Provider.GetAll is async; base class contract is sync. Thread-pool thread, no SyncContext.
        var themesResult = _provider.Get(CancellationToken.None).GetAwaiter().GetResult();

        foreach (var theme in themesResult.Value ?? [])
        {
            theme.IsDefault = string.Equals(theme.Name, name, StringComparison.OrdinalIgnoreCase);

            var saveResult = _provider.Save(theme, CancellationToken.None).GetAwaiter().GetResult();
            if (!saveResult.IsSuccess)
            {
                ThemeLog.ThemePersistenceFailed(_logger, theme.Name, saveResult.CurrentMessage ?? "save failed");
                ThrowError("Failed to persist theme configuration", 500);
                return;
            }
        }

        ThemeLog.DefaultThemeSet(_logger, name);
    }
}
