using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceConnections.FileSystem.Abstractions.Results.Codes;

/// <summary>
/// The resolved path is outside the connection Root directory.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FileSystemResultCodes), "PathOutsideRoot", RestrictToCurrentCompilation = true)]
public sealed class PathOutsideRootCode : FileSystemResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathOutsideRootCode"/> class.
    /// </summary>
    public PathOutsideRootCode()
        : base(
            20001,
            "PathOutsideRoot",
            ResultSeverities.ByName("Error"),
            "Path {path} is outside connection Root {root}")
    {
    }
}
