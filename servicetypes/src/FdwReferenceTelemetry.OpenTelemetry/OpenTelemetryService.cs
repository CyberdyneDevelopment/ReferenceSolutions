using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Telemetry.Abstractions;

namespace FdwReferenceTelemetry.OpenTelemetry;

/// <summary>The configured telemetry pipeline, resolved by name.</summary>
public sealed class OpenTelemetryService : ITelemetryService
{
    private readonly OpenTelemetryConfiguration _configuration;

    /// <summary>Initializes a new instance of the <see cref="OpenTelemetryService"/> class.</summary>
    /// <param name="configuration">The pipeline settings this host was built from.</param>
    public OpenTelemetryService(OpenTelemetryConfiguration configuration)
        => _configuration = configuration;

    /// <summary>Gets the settings this host was built from.</summary>
    public OpenTelemetryConfiguration Configuration => _configuration;

    /// <summary>Gets the identifier of this host.</summary>
    public string Id => _configuration.Name;

    /// <summary>Gets the name this host was resolved by.</summary>
    public string Name => _configuration.Name;

    /// <inheritdoc/>
    public string ServiceType => "Telemetry";

    /// <summary>Gets a value indicating whether the host is usable.</summary>
    public bool IsAvailable => true;

    // Why unsupported: a host configuration has no command surface. Returning success for a command
    // nothing executed would report work that never happened.
    /// <summary>Not supported — a host configuration executes no commands.</summary>
    /// <param name="command">The command that cannot be executed.</param>
    /// <param name="cancellationToken">Unused.</param>
    /// <returns>A failure describing that this service has no command surface.</returns>
    public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult>(GenericResult.Failure(NoCommands()));

    /// <summary>Not supported — a host configuration executes no commands.</summary>
    /// <typeparam name="T">The result type that will not be produced.</typeparam>
    /// <param name="command">The command that cannot be executed.</param>
    /// <param name="cancellationToken">Unused.</param>
    /// <returns>A failure describing that this service has no command surface.</returns>
    public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult<T>>(GenericResult<T>.Failure(NoCommands()));

    private GenericMessage NoCommands()
        => new GenericMessage(
            MessageSeverity.Error,
            $"telemetry pipeline '{Name}' has no command surface; Execute is not supported.");
}
