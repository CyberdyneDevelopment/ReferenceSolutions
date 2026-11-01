using Fdw.Results;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services;
using System.Threading;
using System.Threading.Tasks;

namespace FdwReferenceCredentials.Sql.Registration;

/// <summary>
/// Builds the SqlCredentialService implementation.
/// </summary>
/// <remarks>
/// Resolves nothing, so it completes synchronously. The provider contract is asynchronous because
/// RESOLVING may be; an implementation with nothing to fetch has nothing to await.
/// </remarks>
public sealed class SqlCredentialServiceProvider : ImplementationServiceProviderBase<ICredentialService, ICredentialServiceImplementationConfiguration>, ISqlCredentialServiceProvider
{
    private readonly ISqlCredentialServiceFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="SqlCredentialServiceProvider"/> class.</summary>
    /// <param name="factory">Builds the service.</param>
    public SqlCredentialServiceProvider(ISqlCredentialServiceFactory factory) => _factory = factory;

    /// <inheritdoc />
    public override Task<IGenericResult<ICredentialService>> Create(
        ICredentialServiceImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        // The implementation's factory hands back its own concrete service; the domain is promised
        // its contract, so widen here rather than making every caller cast down.
        var created = _factory.Create(configuration);
        return Task.FromResult(created.IsSuccess && created.Value is not null
            ? GenericResult<ICredentialService>.Success(created.Value)
            : created.ToNewResult<ICredentialService>());
    }
}
