using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.UI.Themes;
using Fdw.UI.Themes.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceConfiguration.Endpoints.Logging;

namespace FdwReferenceConfiguration.Endpoints;

/// <summary>
/// Sets a theme as the default in database-backed configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public class SetDefaultThemeEndpoint : SetDefaultThemeEndpointBase
{
    private readonly ILogger<SetDefaultThemeEndpoint> _logger;
    private readonly IThemeConfigurationProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetDefaultThemeEndpoint"/> class.
    /// </summary>
    public SetDefaultThemeEndpoint(
        ILogger<SetDefaultThemeEndpoint> logger,
        IThemeConfigurationProvider provider)
        : base(logger)
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

    protected override async Task<IGenericResult> SetDefault(string name, CancellationToken ct)
    {
        ThemeLog.SettingDefaultTheme(_logger, name);

        return await _provider.SetDefault(name, ct).ConfigureAwait(false);
    }
}
