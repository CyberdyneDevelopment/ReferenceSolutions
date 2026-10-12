using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace FdwReferenceConnections.FileSystem.Abstractions.Results;

/// <summary>
/// TypeCollection for FileSystem connection result codes.
/// Result codes use categorized catalog numbers (Code "FS-{number}", Id == EventId == number).
/// </summary>
[TypeCollection(typeof(FileSystemResultCodeBase), typeof(IResultCode), typeof(FileSystemResultCodes))]
public abstract partial class FileSystemResultCodes : TypeCollectionBase<FileSystemResultCodeBase, IResultCode>
{
}
