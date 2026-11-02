using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using System.Threading.Tasks;
using System.Threading;
using Fdw.Services;

namespace FdwReferenceConnections.FileSystem.Registration;

/// <summary>
/// Builds the FileSystemConnection implementation.
/// </summary>
/// <remarks>
/// Resolves nothing, so it completes synchronously. The provider contract is asynchronous because
/// RESOLVING may be; an implementation with nothing to fetch has nothing to await.
/// </remarks>
public sealed class FileSystemConnectionProvider : ImplementationServiceProviderBase<IGenericConnection, IConnectionImplementationConfiguration>, IFileSystemConnectionProvider
{
    private readonly IFileSystemConnectionFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="FileSystemConnectionProvider"/> class.</summary>
    /// <param name="factory">Builds the service.</param>
    public FileSystemConnectionProvider(IFileSystemConnectionFactory factory) => _factory = factory;

    /// <inheritdoc />
    public override Task<IGenericResult<IGenericConnection>> Create(
        IConnectionImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        // The implementation's factory hands back its own concrete service; the domain is promised
        // its contract, so widen here rather than making every caller cast down.
        var created = _factory.Create(configuration);
        return Task.FromResult(created.IsSuccess && created.Value is not null
            ? GenericResult<IGenericConnection>.Success(created.Value)
            : created.ToNewResult<IGenericConnection>());
    }
}
