using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace FdwReferenceData.FileSystem;

/// <summary>
/// The native FileSystem command that writes records to a configured file container through the
/// config-driven record writer factory. Carries the rows the translator extracted from the source
/// command's input data.
/// </summary>
public sealed class FileSystemWriteCommand : FileSystemRecordCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemWriteCommand"/> class.
    /// </summary>
    /// <param name="relativePath">The file path relative to the connection root.</param>
    /// <param name="container">The configured container to write.</param>
    /// <param name="rows">The rows to serialize as flat name→value maps.</param>
    public FileSystemWriteCommand(
        string relativePath,
        IDataContainer container,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
        : base(relativePath, container)
    {
        Rows = rows;
    }

    /// <summary>
    /// Gets the rows to write as flat name→value maps.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; }
}
