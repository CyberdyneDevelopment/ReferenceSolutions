using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using FdwReferenceData.FileSystem.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceData.FileSystem;

/// <summary>
/// The translator the file system connection answers for a command type <see cref="FileSystemCommandTranslator"/>
/// does not handle. Its name is the "NotFound" sentinel <c>ConnectionBase</c> reads as "no translator for this
/// command", which lets a command that expands into simpler ones (Upsert, BulkUpsert, BulkUpdate) run those, and fails
/// any other command with <c>TranslatorNotFound</c>. It never builds a native command.
/// </summary>
public sealed class FileSystemTranslatorNotFound : IDataCommandTranslator<IFileSystemCommand>
{
    private readonly string _connectionName;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemTranslatorNotFound"/> class.
    /// </summary>
    /// <param name="connectionName">The owning connection name, for structured logging.</param>
    /// <param name="logger">Logger; falls back to <see cref="NullLogger"/> when null.</param>
    public FileSystemTranslatorNotFound(string connectionName, ILogger? logger)
    {
        _connectionName = connectionName;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public int Id { get; } = OptionId.DeriveInt(typeof(FileSystemTranslatorNotFound).FullName!);

    /// <inheritdoc />
    public string Name => "NotFound";

    /// <inheritdoc />
    public string Category => "FileSystem";

    /// <inheritdoc />
    public string DomainName => "File";

    /// <inheritdoc />
    public Task<IGenericResult<IFileSystemCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
        => Task.FromResult(
            GenericResult<IFileSystemCommand>.Failure(
                FileSystemConfigurationWriteLog.UnrecognizedCommandType(_logger, _connectionName, command.CommandType)));
}
