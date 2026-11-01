using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Logging.Abstractions;
using Serilog.Core;

namespace FdwReferenceLogging.Serilog;

/// <summary>A configured Serilog pipeline, owned by the provider that resolved it.</summary>
public sealed class SerilogLoggingService : ILoggingService, IDisposable
{
    private readonly SerilogLoggingConfiguration _configuration;
    private readonly string _id;

    /// <summary>Initializes a new instance of the <see cref="SerilogLoggingService"/> class.</summary>
    /// <param name="configuration">The settings this pipeline was built from.</param>
    /// <param name="logger">The built Serilog logger.</param>
    public SerilogLoggingService(SerilogLoggingConfiguration configuration, Logger logger)
    {
        _configuration = configuration;
        Logger = logger;
        _id = configuration.Id == Guid.Empty
            ? configuration.Name
            : configuration.Id.ToString("D", CultureInfo.InvariantCulture);
    }

    /// <summary>Gets the built Serilog logger.</summary>
    public Logger Logger { get; }

    /// <summary>Gets the identifier of this pipeline.</summary>
    public string Id => _id;

    /// <summary>Gets the name this pipeline was resolved by.</summary>
    public string Name => _configuration.Name;

    /// <inheritdoc/>
    public string ServiceType => "Logging";

    /// <summary>Gets a value indicating whether the pipeline is usable.</summary>
    public bool IsAvailable => true;

    // Why unsupported rather than a no-op success: a logging pipeline has no command surface, and
    // returning success for a command nothing executed would report work that never happened.
    /// <summary>Not supported — a logging pipeline executes no commands.</summary>
    /// <param name="command">The command that cannot be executed.</param>
    /// <param name="cancellationToken">Unused.</param>
    /// <returns>A failure describing that this service has no command surface.</returns>
    public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult>(GenericResult.Failure(
            SerilogLoggingServiceLogger.CommandsNotSupported(Name)));

    /// <summary>Not supported — a logging pipeline executes no commands.</summary>
    /// <typeparam name="T">The result type that will not be produced.</typeparam>
    /// <param name="command">The command that cannot be executed.</param>
    /// <param name="cancellationToken">Unused.</param>
    /// <returns>A failure describing that this service has no command surface.</returns>
    public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult<T>>(GenericResult<T>.Failure(
            SerilogLoggingServiceLogger.CommandsNotSupported(Name)));

    /// <summary>Disposes the underlying logger, flushing its sinks.</summary>
    public void Dispose() => Logger.Dispose();
}
