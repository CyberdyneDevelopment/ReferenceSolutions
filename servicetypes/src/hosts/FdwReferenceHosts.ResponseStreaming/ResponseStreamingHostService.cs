using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Hosts.Abstractions;
using FdwReferenceHosts.ResponseStreaming.Configuration;
using FdwReferenceHosts.ResponseStreaming.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceHosts.ResponseStreaming;

/// <summary>
/// The response-streaming host option's service: it carries the resolved limits for the streaming endpoints to apply.
/// </summary>
/// <remarks>
/// It holds the limits rather than applying them. The endpoints that stream are what enforce them, and
/// they receive the limits as options built from this record - so no endpoint knows where the limits
/// are stored, and none can pick its own.
/// </remarks>
public sealed class ResponseStreamingHostService : IHostService
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ResponseStreamingHostService"/> class.</summary>
    /// <param name="configuration">The resolved response-streaming settings.</param>
    /// <param name="logger">The logger for this service.</param>
    public ResponseStreamingHostService(ResponseStreamingHostConfiguration configuration, ILogger<ResponseStreamingHostService>? logger = null)
    {
        Configuration = configuration;
        _logger = logger ?? NullLogger<ResponseStreamingHostService>.Instance;
        Id = configuration.Id.ToString();
    }

    /// <inheritdoc />
    public string Name => "ResponseStreaming";

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc/>
    public string ServiceType => "ResponseStreaming";

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <summary>Gets the resolved response-streaming settings.</summary>
    public ResponseStreamingHostConfiguration Configuration { get; }

    // Why a failure rather than a no-op: this option carries a policy, it executes nothing. A
    // caller sending it a command has the wrong service, and returning success would hide that.
    /// <inheritdoc />
    public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult>(
            GenericResult.Failure(ResponseStreamingHostLog.ServiceExecutesNothing(_logger, command?.GetType().Name ?? "(null)")));

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(
            GenericResult<T>.Failure(ResponseStreamingHostLog.ServiceExecutesNothing(_logger, command?.GetType().Name ?? "(null)")));
}
