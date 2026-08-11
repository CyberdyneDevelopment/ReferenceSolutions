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
