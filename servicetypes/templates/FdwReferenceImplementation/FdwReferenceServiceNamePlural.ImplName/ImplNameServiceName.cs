using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceServiceNamePlural.ImplName;

/// <summary>
/// The ImplName ServiceName — the aggregation over FDW's ImplName machinery.
/// </summary>
// Why: this class COMPOSES framework pieces; it does not reimplement them. Everything it leans on
// stays in FDW and is consumed as public API, because a reference implementation gets no
// InternalsVisibleTo — and that constraint is what proves the framework's surface is sufficient.
public sealed class ImplNameServiceName
{
    private readonly ILogger<ImplNameServiceName> _logger;
    private readonly string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImplNameServiceName"/> class.
    /// </summary>
    /// <param name="name">The configured name of this service instance.</param>
    /// <param name="logger">Receives structured diagnostics; may be null when DI has no logging.</param>
    public ImplNameServiceName(string name, ILogger<ImplNameServiceName>? logger)
    {
        _name = name;

        // Why: the ONLY permitted ?? fallback in this codebase — it keeps the class usable when DI
        // has not wired logging. Never use ?? to substitute a missing domain value; fail loud instead.
        _logger = logger ?? NullLogger<ImplNameServiceName>.Instance;
    }

    /// <summary>
    /// Gets the configured name of this service instance.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Performs the service's primary operation.
    /// </summary>
    /// <param name="cancellationToken">Propagated to every async callee.</param>
    /// <returns>A result describing success or a structured failure.</returns>
    public Task<IGenericResult> Execute(CancellationToken cancellationToken = default)
    {
        // TODO: compose the FDW machinery here, and add a MessageLogging class for this service --
        // see any Fdw.Services.*/Logging/*Log.cs for the shape. FDW code logs through generated
        // MessageLogging methods, never raw ILogger calls, because the generated method returns the
        // IGenericMessage that a failing IGenericResult carries: the log line and the failure are
        // the same object.
        //
        // When an input is missing, return GenericResult.Failure(SomeLog.Method(_logger, ...)).
        // Never substitute a default and carry on.
        _ = _logger;
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
