using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services;
using System.Threading;
using System.Threading.Tasks;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Registration;

/// <summary>
/// Builds the EnvironmentVariableSecretManager implementation.
/// </summary>
/// <remarks>
/// Resolves nothing, so it completes synchronously. The provider contract is asynchronous because
/// RESOLVING may be; an implementation with nothing to fetch has nothing to await.
/// </remarks>
public sealed class EnvironmentVariableSecretManagerProvider : ImplementationServiceProviderBase<ISecretManager, ISecretManagerImplementationConfiguration>, IEnvironmentVariableSecretManagerProvider
{
    private readonly IEnvironmentVariableSecretManagerFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="EnvironmentVariableSecretManagerProvider"/> class.</summary>
    /// <param name="factory">Builds the service.</param>
    public EnvironmentVariableSecretManagerProvider(IEnvironmentVariableSecretManagerFactory factory) => _factory = factory;

    /// <inheritdoc />
    public override Task<IGenericResult<ISecretManager>> Create(
        ISecretManagerImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        // The implementation's factory hands back its own concrete service; the domain is promised
        // its contract, so widen here rather than making every caller cast down.
        var created = _factory.Create(configuration);
        return Task.FromResult(created.IsSuccess && created.Value is not null
            ? GenericResult<ISecretManager>.Success(created.Value)
            : created.ToNewResult<ISecretManager>());
    }
}
