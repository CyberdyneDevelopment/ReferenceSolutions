using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Hosts.Abstractions;
using FdwReferenceHosts.ResponseBuffering.Configuration;
using FdwReferenceHosts.ResponseBuffering.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceHosts.ResponseBuffering;

/// <summary>
/// The response-buffering host option's service: it carries the resolved settings for the host to apply.
/// </summary>
/// <remarks>
/// It holds the settings rather than applying them. Where buffering sits in the request pipeline -
/// inside the exception handler, outside anything that sets Content-Length - is the host's decision,
/// and only the host knows its own order.
/// </remarks>
public sealed class ResponseBufferingHostService : IHostService
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ResponseBufferingHostService"/> class.</summary>
    /// <param name="configuration">The resolved response-buffering settings.</param>
    /// <param name="logger">The logger for this service.</param>
    public ResponseBufferingHostService(ResponseBufferingHostConfiguration configuration, ILogger<ResponseBufferingHostService>? logger = null)
    {
        Configuration = configuration;
        _logger = logger ?? NullLogger<ResponseBufferingHostService>.Instance;
        Id = configuration.Id.ToString();
    }

    /// <inheritdoc />
    public string Name => "ResponseBuffering";

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc/>
    public string ServiceType => "ResponseBuffering";

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <summary>Gets the resolved response-buffering settings.</summary>
    public ResponseBufferingHostConfiguration Configuration { get; }

    // Why a failure rather than a no-op: this option carries a policy, it executes nothing. A
    // caller sending it a command has the wrong service, and returning success would hide that.
    /// <inheritdoc />
    public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult>(
            GenericResult.Failure(ResponseBufferingHostLog.ServiceExecutesNothing(_logger, command?.GetType().Name ?? "(null)")));

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(
            GenericResult<T>.Failure(ResponseBufferingHostLog.ServiceExecutesNothing(_logger, command?.GetType().Name ?? "(null)")));
}
