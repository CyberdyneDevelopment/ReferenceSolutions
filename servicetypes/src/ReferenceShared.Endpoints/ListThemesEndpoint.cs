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
