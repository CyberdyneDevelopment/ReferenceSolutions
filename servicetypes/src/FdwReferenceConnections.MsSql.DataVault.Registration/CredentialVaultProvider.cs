using Fdw.Results;
using Fdw.Services.DataVault.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services;

namespace FdwReferenceConnections.MsSql.DataVault.Registration;

/// <summary>
/// Builds the CredentialVault implementation.
/// </summary>
/// <remarks>
/// Resolves nothing, so it completes synchronously. The provider contract is asynchronous because
/// RESOLVING may be; an implementation with nothing to fetch has nothing to await.
/// </remarks>
public sealed class CredentialVaultProvider : ImplementationServiceProviderBase<IDataVault, IDataVaultImplementationConfiguration>, ICredentialVaultProvider
{
    private readonly ICredentialVaultFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="CredentialVaultProvider"/> class.</summary>
    /// <param name="factory">Builds the service.</param>
    public CredentialVaultProvider(ICredentialVaultFactory factory) => _factory = factory;

    /// <inheritdoc />
    public override async Task<IGenericResult<IDataVault>> Create(
        IDataVaultImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        // The DataVault domain's factory is still asynchronous: it resolves a connection, a secret
        // manager and a pepper. Converting it is the same move as the connection factories and has
        // not been done yet, so this awaits rather than pretending otherwise.
        var created = await _factory.Create(configuration, cancellationToken).ConfigureAwait(false);
        return created.IsSuccess && created.Value is not null
            ? GenericResult<IDataVault>.Success(created.Value)
            : created.ToNewResult<IDataVault>();
    }
}
