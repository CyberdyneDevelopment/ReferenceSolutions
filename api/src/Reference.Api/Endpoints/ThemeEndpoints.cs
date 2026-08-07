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
using Reference.Api.Logging;

namespace Reference.Api.Endpoints;

#region List Themes Endpoint

/// <summary>
/// Lists all available themes from database-backed configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListThemesEndpoint : ListThemesEndpoint<ThemeSummaryPayload>
{
    private readonly ILogger<ListThemesEndpoint> _logger;
    // Why: ThemeConfigurationProvider replaces IOptionsMonitor<List<T>> — provides dual-source
    // (ctrl + cfg) theme resolution through DefaultConfigurationProvider pattern.
    private readonly ThemeConfigurationProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListThemesEndpoint"/> class.
    /// </summary>
    public ListThemesEndpoint(
        ILogger<ListThemesEndpoint> logger,
        ThemeConfigurationProvider provider)
    {
        _logger = logger ?? NullLogger<ListThemesEndpoint>.Instance;
        _provider = provider;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/themes");
        Policies("configurations:read");
        Summary(s =>
        {
            s.Summary = "List available themes";
            s.Description = "Returns a list of all available UI themes.";
        });
    }

    protected override List<ThemeSummaryPayload> LoadThemes()
    {
        ThemeLog.ListingThemes(_logger);

        // Why: GetAll() is async but base class contract is sync. Thread-pool thread, no SyncContext.
        var allThemesResult = _provider.Get(CancellationToken.None).GetAwaiter().GetResult();
        var themes = (allThemesResult.Value ?? [])
            .Select(ThemeConfigurationMapper.ToSummary)
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        ThemeLog.ThemesRetrieved(_logger, themes.Count);
        return themes;
    }
}

#endregion

#region Get Theme Endpoint

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

#endregion

#region Get Default Theme Endpoint

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

#endregion

#region Create Theme Endpoint

/// <summary>
/// Creates a new theme and persists to database.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateThemeEndpoint : CreateThemeEndpoint<CreateThemeRequest, ThemeConfiguration>
{
    private readonly ILogger<CreateThemeEndpoint> _logger;
    // Why: ThemeConfigurationProvider replaces IOptionsMonitor<List<T>> — provides dual-source
    // (ctrl + cfg) theme resolution through DefaultConfigurationProvider pattern.
    private readonly ThemeConfigurationProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateThemeEndpoint"/> class.
    /// </summary>
    public CreateThemeEndpoint(
        ILogger<CreateThemeEndpoint> logger,
        ThemeConfigurationProvider provider)
    {
        _logger = logger ?? NullLogger<CreateThemeEndpoint>.Instance;
        _provider = provider;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/themes");
        Policies("configurations:write");
        Summary(s =>
        {
            s.Summary = "Create a new theme";
            s.Description = "Creates a new UI theme configuration.";
        });
    }

    protected override string GetThemeName(CreateThemeRequest req) => req.Name;

    // Why: Provider.Get is async; base class contract is sync. Thread-pool thread, no SyncContext.
    protected override bool ThemeExists(string name)
    {
        var result = _provider.Get(name, CancellationToken.None).GetAwaiter().GetResult();
        return result.IsSuccess && result.Value is not null;
    }

    protected override ThemeConfiguration CreateTheme(CreateThemeRequest req)
    {
        // Why: HandleAsync in the base class calls CreateTheme() synchronously, but persistence is async.
        // We use GetAwaiter().GetResult() here because the base class contract is synchronous.
        // This is acceptable only because the endpoint is already running on a thread-pool thread
        // (FastEndpoints uses ASP.NET Core middleware) and no SynchronizationContext is present.
        var managed = req.ToManaged();
        ThemeLog.CreatingTheme(_logger, req.Name);

        var saveResult = _provider.Save(managed, CancellationToken.None).GetAwaiter().GetResult();
        if (!saveResult.IsSuccess)
        {
            ThemeLog.ThemePersistenceFailed(_logger, req.Name, saveResult.CurrentMessage ?? "save failed");
            ThrowError("Failed to persist theme configuration", 500);
            return default!;
        }

        ThemeLog.ThemeCreated(_logger, req.Name);
        return ThemeConfigurationMapper.ToDto(managed);
    }
}

#endregion

#region Update Theme Endpoint

/// <summary>
/// Request to update a theme, including the route-bound name.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateThemeByNameRequest : UpdateThemeRequest
{
    /// <summary>
    /// Gets or sets the theme name (from route).
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

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
        Put("/themes/{Name}");
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

#endregion

#region Delete Theme Endpoint

/// <summary>
/// Deletes a theme from database-backed configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public class DeleteThemeEndpoint : Fdw.UI.Themes.Endpoints.DeleteThemeEndpoint
{
    private readonly ILogger<DeleteThemeEndpoint> _logger;
    private readonly ThemeConfigurationProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteThemeEndpoint"/> class.
    /// </summary>
    public DeleteThemeEndpoint(
        ILogger<DeleteThemeEndpoint> logger,
        ThemeConfigurationProvider provider)
        : base(provider, logger)
    {
        _logger = logger ?? NullLogger<DeleteThemeEndpoint>.Instance;
        _provider = provider;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Delete("/themes/{Name}");
        Policies("configurations:write");
        Summary(s =>
        {
            s.Summary = "Delete a theme";
            s.Description = "Deletes a UI theme. Cannot delete the default theme or built-in themes.";
        });
    }

    protected override bool CanDelete(string name, out string? reason)
    {
        // Why: Provider.Get is async; base class contract is sync. Thread-pool thread, no SyncContext.
        var result = _provider.Get(name, CancellationToken.None).GetAwaiter().GetResult();

        if (!result.IsSuccess || result.Value == null)
        {
            reason = "not-found";
            return false;
        }

        var existing = result.Value;

        if (existing.IsDefault)
        {
            ThemeLog.CannotDeleteDefaultTheme(_logger, existing.Name);
            reason = "Cannot delete the default theme. Set another theme as default first.";
            return false;
        }

        var builtInThemes = new[] { "fractal", "default-light", "default-dark", "cyberdyne", "devtools", "mc3po" };
        if (builtInThemes.Contains(existing.Name, StringComparer.OrdinalIgnoreCase))
        {
            ThemeLog.CannotDeleteBuiltInTheme(_logger, existing.Name);
            reason = "Cannot delete built-in themes.";
            return false;
        }

        reason = null;
        return true;
    }

}

#endregion

#region Set Default Theme Endpoint

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

#endregion
