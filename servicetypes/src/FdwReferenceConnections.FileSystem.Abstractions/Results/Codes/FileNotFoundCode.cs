using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceConnections.FileSystem.Abstractions.Results.Codes;

/// <summary>
/// The file at the connector path was not found.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FileSystemResultCodes), "FileNotFound", RestrictToCurrentCompilation = true)]
public sealed class FileNotFoundCode : FileSystemResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileNotFoundCode"/> class.
    /// </summary>
    public FileNotFoundCode()
        : base(
            30000,
            "FileNotFound",
            ResultSeverities.ByName("Error"),
            "File not found at connector path {path}")
    {
    }
}
