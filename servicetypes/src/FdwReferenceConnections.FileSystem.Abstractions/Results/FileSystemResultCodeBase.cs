using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace FdwReferenceConnections.FileSystem.Abstractions.Results;

/// <summary>
/// Base class for FileSystem connection result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class FileSystemResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected FileSystemResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemResultCodeBase"/> class.
    /// </summary>
    protected FileSystemResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "FileSystem", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance from a categorized <paramref name="number"/> (catalog scheme).
    /// </summary>
    protected FileSystemResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "FS", isRetryable)
    {
    }
}
