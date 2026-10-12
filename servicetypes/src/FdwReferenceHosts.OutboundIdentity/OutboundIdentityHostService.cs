using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Hosts.Abstractions;
using FdwReferenceHosts.OutboundIdentity.Configuration;
using FdwReferenceHosts.OutboundIdentity.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceHosts.OutboundIdentity;

/// <summary>
/// The outbound-identity host option's service: it carries the resolved identity settings.
/// </summary>
/// <remarks>
/// It holds the settings rather than applying them. The host's outbound HTTP clients attach the token an
/// <c>IAccessTokenProvider</c> built from this record obtains, so no client knows where the identity is stored,
/// and none can pick its own.
/// </remarks>
public sealed class OutboundIdentityHostService : IHostService
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="OutboundIdentityHostService"/> class.</summary>
    /// <param name="configuration">The resolved outbound-identity settings.</param>
    /// <param name="logger">The logger for this service.</param>
    public OutboundIdentityHostService(OutboundIdentityHostConfiguration configuration, ILogger<OutboundIdentityHostService>? logger = null)
    {
        Configuration = configuration;
        _logger = logger ?? NullLogger<OutboundIdentityHostService>.Instance;
        Id = configuration.Id.ToString();
    }

    /// <inheritdoc />
    public string Name => "OutboundIdentity";

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc/>
    public string ServiceType => "OutboundIdentity";

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <summary>Gets the resolved outbound-identity settings.</summary>
    public OutboundIdentityHostConfiguration Configuration { get; }

    // Why a failure rather than a no-op: this option carries a policy, it executes nothing. A
    // caller sending it a command has the wrong service, and returning success would hide that.
    /// <inheritdoc />
    public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult>(
            GenericResult.Failure(OutboundIdentityHostLog.ServiceExecutesNothing(_logger, command?.GetType().Name ?? "(null)")));

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(
            GenericResult<T>.Failure(OutboundIdentityHostLog.ServiceExecutesNothing(_logger, command?.GetType().Name ?? "(null)")));
}
