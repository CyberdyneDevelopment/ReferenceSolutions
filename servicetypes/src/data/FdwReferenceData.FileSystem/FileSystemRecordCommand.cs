using Fdw.Data.Abstractions;

namespace FdwReferenceData.FileSystem;

/// <summary>
/// Base for the native FileSystem commands produced by <see cref="FileSystemCommandTranslator"/> from an
/// <c>IDataCommand</c> + container. A native command carries the resolved relative file path and the
/// configured container the <c>FileSystemRecordConnector</c> reads/writes; the concrete subtype
/// (<see cref="FileSystemReadCommand"/> / <see cref="FileSystemWriteCommand"/>) is the I/O direction.
/// </summary>
/// <remarks>
/// Why a native command at all: <c>ConnectionBase</c> routes <c>Execute(IDataCommand, IDataContainer)</c>
/// through a translator to a native command type (here <see cref="IFileSystemCommand"/>),
/// then runs <c>Execute&lt;T&gt;(nativeCommand, IStorageContainer)</c>. This is the same seam MsSql (SqlCommand)
/// and Http (HttpRequestMessage) use; the FileSystem native command is the file-I/O instruction carrying
/// the path the translator computed from the container's <see cref="IStorageContainer.Path"/>.
/// <para>
/// Why two subtypes rather than a direction flag: read and write are distinct operations with distinct
/// payloads (a write carries rows, a read does not), so the connection dispatches by pattern-matching the
/// command type — no enum/flag discriminator and no switch on a direction value.
/// </para>
/// </remarks>
public abstract class FileSystemRecordCommand : IFileSystemCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemRecordCommand"/> class.
    /// </summary>
    /// <param name="relativePath">The file path relative to the connection root, resolved from the container.</param>
    /// <param name="container">The configured container (format + field schema) to read/write.</param>
    protected FileSystemRecordCommand(string relativePath, IDataContainer container)
    {
        RelativePath = relativePath;
        Container = container;
    }

    /// <summary>
    /// Gets the file path relative to the connection root.
    /// </summary>
    public string RelativePath { get; }

    /// <summary>
    /// Gets the configured container to read/write through the record source/writer factory.
    /// </summary>
    public IDataContainer Container { get; }
}
