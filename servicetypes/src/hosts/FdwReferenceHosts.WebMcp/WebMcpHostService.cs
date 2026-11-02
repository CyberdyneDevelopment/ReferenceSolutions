using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Hosts.Abstractions;
using FdwReferenceHosts.WebMcp.Configuration;
using FdwReferenceHosts.WebMcp.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceHosts.WebMcp;

/// <summary>
/// The WebMCP host option's service: it carries the resolved settings for the host to apply.
/// </summary>
/// <remarks>
/// It holds the policy rather than applying it. Where the WebMCP middleware and route sit in the pipeline - the key check before
/// authentication, the script route after the endpoints - is the host's decision, and only the host knows its own order.
/// </remarks>
public sealed class WebMcpHostService : IHostService
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="WebMcpHostService"/> class.</summary>
    /// <param name="configuration">The resolved WebMCP settings.</param>
    /// <param name="logger">The logger for this service.</param>
    public WebMcpHostService(WebMcpHostConfiguration configuration, ILogger<WebMcpHostService>? logger = null)
    {
        Configuration = configuration;
        _logger = logger ?? NullLogger<WebMcpHostService>.Instance;
        Id = configuration.Id.ToString();
    }

    /// <inheritdoc />
    public string Name => "WebMcp";

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc/>
    public string ServiceType => "WebMcp";

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <summary>Gets the resolved CORS policy.</summary>
    public WebMcpHostConfiguration Configuration { get; }

    // Why a failure rather than a no-op: this option carries settings, it executes nothing. A
    // caller sending it a command has the wrong service, and returning success would hide that.
    /// <inheritdoc />
    public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult>(
            GenericResult.Failure(WebMcpHostLog.ServiceExecutesNothing(_logger, command?.GetType().Name ?? "(null)")));

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(
            GenericResult<T>.Failure(WebMcpHostLog.ServiceExecutesNothing(_logger, command?.GetType().Name ?? "(null)")));
}
