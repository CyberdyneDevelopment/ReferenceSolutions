using Fdw.Services;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using FdwReferenceConnections.FileSystem.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.FileSystem;

/// <summary>
/// Factory for creating <see cref="FileSystemConnection"/> instances.
/// Validates Root is non-empty and the directory exists before building the connection.
/// </summary>
public sealed class FileSystemConnectionFactory : ServiceFactoryBase<IGenericConnection, FileSystemConnectionConfiguration>, IFileSystemConnectionFactory
{
    private readonly ILogger<FileSystemConnectionFactory> _logger;
    private readonly ILogger<FileSystemConnection> _connectionLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemConnectionFactory"/> class.
    /// </summary>
    public FileSystemConnectionFactory(
        ILogger<FileSystemConnectionFactory> logger,
        ILogger<FileSystemConnection> connectionLogger)
    {
        _logger = logger ?? NullLogger<FileSystemConnectionFactory>.Instance;
        _connectionLogger = connectionLogger ?? NullLogger<FileSystemConnection>.Instance;
    }



    /// <inheritdoc />
    public Task<IGenericResult<IGenericConnection>> Create(
        IGenericConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        // Why: FileSystemConnectionConfiguration declares no AuthenticationType and there is no
        // FileSystem authentication TypeCollection, so there is no secret to resolve and nothing for a
        // secret-manager provider to do. A remote transport (SFTP) would add an authentication type
        // first; the provider becomes a constructor dependency at that point, not before.
        // CreateInternal rather than Create: the typed Create is an override, so it does not take
        // part in overload resolution at this level -- a bare Create(configuration) binds to the
        // async Create(IGenericConfiguration, CancellationToken) declared here and returns a Task.
        return Task.FromResult(
            configuration is FileSystemConnectionConfiguration typed
                ? CreateInternal(typed, typed.Name)
                : GenericResult<IGenericConnection>.Failure(
                    FileSystemConnectionLog.FactoryValidationFailed(
                        _logger,
                        string.Empty,
                        $"expected a FileSystemConnectionConfiguration, got '{configuration?.GetType().Name ?? "null"}'")));
    }

    /// <inheritdoc />
    public override IGenericResult<IGenericConnection> Create(FileSystemConnectionConfiguration configuration)
        => CreateInternal(configuration, configuration.Name);

    private IGenericResult<IGenericConnection> CreateInternal(
        FileSystemConnectionConfiguration configuration, string connectionName)
    {
        if (string.IsNullOrWhiteSpace(configuration.Root))
            return GenericResult<IGenericConnection>.Failure(
                FileSystemConnectionLog.FactoryValidationFailed(
                    _logger, connectionName.Length > 0 ? connectionName : configuration.Id.ToString(),
                    "Root directory is required but was empty or whitespace"));

        var canonicalRoot = Path.GetFullPath(configuration.Root);

        if (!Directory.Exists(canonicalRoot))
            return GenericResult<IGenericConnection>.Failure(
                FileSystemConnectionLog.FactoryValidationFailed(
                    _logger, connectionName.Length > 0 ? connectionName : configuration.Id.ToString(),
                    $"Root directory does not exist: {canonicalRoot}"));

        return GenericResult<IGenericConnection>.Success(
            new FileSystemConnection(configuration, _connectionLogger));
    }

}
