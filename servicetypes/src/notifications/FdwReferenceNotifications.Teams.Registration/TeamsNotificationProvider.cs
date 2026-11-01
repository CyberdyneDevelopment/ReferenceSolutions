using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services;

namespace FdwReferenceNotifications.Teams.Registration;

/// <summary>
/// Builds the TeamsNotification implementation.
/// </summary>
/// <remarks>
/// Resolves nothing, so it completes synchronously. The provider contract is asynchronous because
/// RESOLVING may be; an implementation with nothing to fetch has nothing to await.
/// </remarks>
public sealed class TeamsNotificationProvider : ImplementationServiceProviderBase<IPlatformNotification, INotificationImplementationConfiguration>, ITeamsNotificationProvider
{
    private readonly ITeamsNotificationFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="TeamsNotificationProvider"/> class.</summary>
    /// <param name="factory">Builds the service.</param>
    public TeamsNotificationProvider(ITeamsNotificationFactory factory) => _factory = factory;

    /// <inheritdoc />
    public override Task<IGenericResult<IPlatformNotification>> Create(
        INotificationImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        // The implementation's factory hands back its own concrete service; the domain is promised
        // its contract, so widen here rather than making every caller cast down.
        var created = _factory.Create(configuration);
        return Task.FromResult(created.IsSuccess && created.Value is not null
            ? GenericResult<IPlatformNotification>.Success(created.Value)
            : created.ToNewResult<IPlatformNotification>());
    }
}
