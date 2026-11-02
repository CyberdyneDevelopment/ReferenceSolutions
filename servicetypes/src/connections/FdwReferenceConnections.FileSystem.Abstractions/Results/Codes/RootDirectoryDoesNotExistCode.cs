using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceConnections.FileSystem.Abstractions.Results.Codes;

/// <summary>
/// The FileSystemConnection Root directory does not exist on disk.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FileSystemResultCodes), "RootDirectoryDoesNotExist", RestrictToCurrentCompilation = true)]
public sealed class RootDirectoryDoesNotExistCode : FileSystemResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RootDirectoryDoesNotExistCode"/> class.
    /// </summary>
    public RootDirectoryDoesNotExistCode()
        : base(
            60004,
            "RootDirectoryDoesNotExist",
            ResultSeverities.ByName("Error"),
            "FileSystemConnection {connection} Root directory does not exist: {root}")
    {
    }
}
