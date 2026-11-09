using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Fdw.UI.Themes;
using Fdw.UI.Themes.Clients.Models;
using Fdw.UI.Themes.Configuration;
using Fdw.UI.Themes.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceConfiguration.Endpoints.Logging;
using Fdw.Results;
using System;

namespace FdwReferenceConfiguration.Endpoints;

/// <summary>
/// Creates a new theme and persists to database.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateThemeEndpoint : CreateThemeEndpointBase<CreateThemeRequest, ThemeConfiguration>
{
    private readonly ILogger<CreateThemeEndpoint> _logger;
    // Why: IThemeConfigurationProvider replaces IOptionsMonitor<List<T>> — provides dual-source
    // (ctrl + cfg) theme resolution through ImplementationConfigurationProviderBase pattern.
    private readonly IThemeConfigurationProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateThemeEndpoint"/> class.
    /// </summary>
    public CreateThemeEndpoint(
        ILogger<CreateThemeEndpoint> logger,
        IThemeConfigurationProvider provider)
        : base(logger)
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
        // A value the request leaves out takes the default theme's (FDW-3823): the default theme is configuration.
        var defaultTheme = _provider.GetDefault(CancellationToken.None).GetAwaiter().GetResult();
        if (defaultTheme.IsFailure || defaultTheme.Value is not { } theme)
        {
            ThrowError(defaultTheme.NoValueReason().Message, 400);
            return default!;
        }

        var managed = req.ToManaged(theme);

        ThemeLog.CreatingTheme(_logger, req.Name);

        var saveResult = _provider.Save(managed, "Theme", managed.Implementation, managed.Name, CancellationToken.None).GetAwaiter().GetResult();
        if (!saveResult.IsSuccess)
        {
            ThemeLog.ThemePersistenceFailed(_logger, req.Name, saveResult.FailureReason().Message);
            ThrowError("Failed to persist theme configuration", 500);
            return default!;
        }

        ThemeLog.ThemeCreated(_logger, req.Name);
        return ThemeConfigurationMapper.ToDto(managed);
    }
}
