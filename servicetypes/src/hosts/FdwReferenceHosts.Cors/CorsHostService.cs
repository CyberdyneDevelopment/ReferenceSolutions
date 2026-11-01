using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Hosts.Abstractions;
using FdwReferenceHosts.Cors.Configuration;
using FdwReferenceHosts.Cors.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceHosts.Cors;

/// <summary>
/// The CORS host option's service: it carries the resolved policy for the host to apply.
/// </summary>
/// <remarks>
/// It holds the policy rather than applying it. Where CORS sits in the request pipeline - after
/// routing, before authorization - is the host's decision, and only the host knows its own order.
/// </remarks>
public sealed class CorsHostService : IHostService
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="CorsHostService"/> class.</summary>
    /// <param name="configuration">The resolved CORS policy.</param>
    /// <param name="logger">The logger for this service.</param>
    public CorsHostService(CorsHostConfiguration configuration, ILogger<CorsHostService>? logger = null)
    {
        Configuration = configuration;
        _logger = logger ?? NullLogger<CorsHostService>.Instance;
        Id = configuration.Id.ToString();
    }

    /// <inheritdoc />
    public string Name => "Cors";

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc/>
    public string ServiceType => "Cors";

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <summary>Gets the resolved CORS policy.</summary>
    public CorsHostConfiguration Configuration { get; }

    // Why a failure rather than a no-op: this option carries a policy, it executes nothing. A
    // caller sending it a command has the wrong service, and returning success would hide that.
    /// <inheritdoc />
    public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult>(
            GenericResult.Failure(CorsHostLog.ServiceExecutesNothing(_logger, command?.GetType().Name ?? "(null)")));

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(
            GenericResult<T>.Failure(CorsHostLog.ServiceExecutesNothing(_logger, command?.GetType().Name ?? "(null)")));
}
