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
/// Updates an existing theme in database-backed configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateThemeEndpoint : UpdateThemeEndpoint<UpdateThemeByNameRequest, ThemeConfiguration>
{
    private readonly ILogger<UpdateThemeEndpoint> _logger;
    private readonly ThemeConfigurationProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateThemeEndpoint"/> class.
    /// </summary>
    public UpdateThemeEndpoint(
        ILogger<UpdateThemeEndpoint> logger,
        ThemeConfigurationProvider provider)
    {
        _logger = logger ?? NullLogger<UpdateThemeEndpoint>.Instance;
        _provider = provider;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Patch("/themes/{Name}");
        Policies("configurations:write");
        Summary(s =>
        {
            s.Summary = "Update a theme";
            s.Description = "Updates an existing UI theme configuration.";
        });
    }

    protected override string GetThemeName(UpdateThemeByNameRequest req) => req.Name;

    protected override ThemeConfiguration? FindTheme(string name)
    {
        // Why: Provider.Get is async; base class contract is sync. Thread-pool thread, no SyncContext.
        var result = _provider.Get(name, CancellationToken.None).GetAwaiter().GetResult();

        if (!result.IsSuccess || result.Value == null)
        {
            ThemeLog.ThemeNotFound(_logger, name);
            return null;
        }

        ThemeLog.UpdatingTheme(_logger, result.Value.Name);

        // Why: return the DTO so the base class can call ApplyUpdate — the managed config is looked
        // up again in ApplyUpdate where we need both the request and the original record.
        return result.Value.ToDto();
    }

    protected override ThemeConfiguration ApplyUpdate(UpdateThemeByNameRequest req, ThemeConfiguration existing)
    {
        // Why: we re-read the managed config here rather than passing it through FindTheme because
        // the base class signature only returns TDetail (ThemeConfiguration DTO), not the managed record.
        var result = _provider.Get(req.Name, CancellationToken.None).GetAwaiter().GetResult();

        if (!result.IsSuccess || result.Value == null)
        {
            ThrowError("Theme not found during update", 404);
            return default!;
        }

        var managed = result.Value;
        managed.ApplyUpdate(req);

        var saveResult = _provider.Save(managed, CancellationToken.None).GetAwaiter().GetResult();
        if (!saveResult.IsSuccess)
        {
            ThemeLog.ThemePersistenceFailed(_logger, managed.Name, saveResult.CurrentMessage ?? "save failed");
            ThrowError("Failed to persist theme configuration", 500);
            return default!;
        }

        ThemeLog.ThemeUpdated(_logger, managed.Name);
        return ThemeConfigurationMapper.ToDto(managed);
    }
}
