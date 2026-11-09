using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.UI.Themes;
using Fdw.UI.Themes.Clients.Models;
using Fdw.UI.Themes.Configuration;
using Fdw.UI.Themes.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceConfiguration.Endpoints.Logging;

namespace FdwReferenceConfiguration.Endpoints;

/// <summary>
/// Gets the default theme from database-backed configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetDefaultThemeEndpoint : GetDefaultThemeEndpointBase<ThemeConfiguration>
{
    private readonly ILogger<GetDefaultThemeEndpoint> _logger;
    private readonly IThemeConfigurationProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetDefaultThemeEndpoint"/> class.
    /// </summary>
    public GetDefaultThemeEndpoint(
        ILogger<GetDefaultThemeEndpoint> logger,
        IThemeConfigurationProvider provider)
        : base(logger)
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

    protected override async Task<IGenericResult<ThemeConfiguration>> LoadDefaultTheme(CancellationToken ct)
    {
        ThemeLog.GettingDefaultTheme(_logger);

        var found = await _provider.GetDefault(ct).ConfigureAwait(false);
        return found.IsSuccess && found.Value is { } managed
            ? GenericResult<ThemeConfiguration>.Success(managed.ToDto())
            : found.ToNewResult<ThemeConfiguration>();
    }
}
