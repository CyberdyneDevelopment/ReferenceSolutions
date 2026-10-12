using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Hosts.Abstractions;
using FdwReferenceHosts.SqlServerEndpoint.Configuration;
using FdwReferenceHosts.SqlServerEndpoint.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceHosts.SqlServerEndpoint;

/// <summary>
/// The SQL Server endpoint host service: carries the endpoint's configuration. The listener does the work.
/// </summary>
public sealed class SqlServerEndpointHostService : IHostService
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="SqlServerEndpointHostService"/> class.</summary>
    /// <param name="configuration">The endpoint's configuration.</param>
    /// <param name="logger">The logger.</param>
    public SqlServerEndpointHostService(SqlServerEndpointHostConfiguration configuration, ILogger<SqlServerEndpointHostService>? logger = null)
    {
        Configuration = configuration;
        _logger = logger ?? NullLogger<SqlServerEndpointHostService>.Instance;
        Id = configuration.Id.ToString();
    }

    /// <inheritdoc/>
    public string Name => "SqlServerEndpoint";

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public string ServiceType => "SqlServerEndpoint";

    /// <inheritdoc/>
    public bool IsAvailable => true;

    /// <summary>Gets the endpoint's configuration.</summary>
    public SqlServerEndpointHostConfiguration Configuration { get; }

    // Why a failure rather than a no-op: this option carries settings, it executes nothing. A caller
    // sending it a command has the wrong service, and returning success would hide that.
    /// <inheritdoc/>
    public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Task.FromResult<IGenericResult>(
            GenericResult.Failure(SqlServerEndpointHostLog.ServiceExecutesNothing(_logger, command.GetType().Name)));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Task.FromResult(
            GenericResult<T>.Failure(SqlServerEndpointHostLog.ServiceExecutesNothing(_logger, command.GetType().Name)));
    }
}
