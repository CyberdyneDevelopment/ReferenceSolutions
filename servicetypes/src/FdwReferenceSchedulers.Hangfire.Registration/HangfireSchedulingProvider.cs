using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services;

namespace FdwReferenceSchedulers.Hangfire.Registration;

/// <summary>
/// Builds the HangfireScheduling implementation.
/// </summary>
/// <remarks>
/// Resolves nothing, so it completes synchronously. The provider contract is asynchronous because
/// RESOLVING may be; an implementation with nothing to fetch has nothing to await.
/// </remarks>
public sealed class HangfireSchedulingProvider : ImplementationServiceProviderBase<IFrameworkSchedulingService, ISchedulerImplementationConfiguration>, IHangfireSchedulingProvider
{
    private readonly IHangfireSchedulingFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="HangfireSchedulingProvider"/> class.</summary>
    /// <param name="factory">Builds the service.</param>
    public HangfireSchedulingProvider(IHangfireSchedulingFactory factory) => _factory = factory;

    /// <inheritdoc />
    public override Task<IGenericResult<IFrameworkSchedulingService>> Create(
        ISchedulerImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        // The implementation's factory hands back its own concrete service; the domain is promised
        // its contract, so widen here rather than making every caller cast down.
        var created = _factory.Create(configuration);
        return Task.FromResult(created.IsSuccess && created.Value is not null
            ? GenericResult<IFrameworkSchedulingService>.Success(created.Value)
            : created.ToNewResult<IFrameworkSchedulingService>());
    }
}
