using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceConnections.FileSystem.Abstractions.Results.Codes;

/// <summary>
/// The requested path resolves outside the connection Root (path traversal attempt).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FileSystemResultCodes), "PathTraversalDenied", RestrictToCurrentCompilation = true)]
public sealed class PathTraversalDeniedCode : FileSystemResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathTraversalDeniedCode"/> class.
    /// </summary>
    public PathTraversalDeniedCode()
        : base(
            50001,
            "PathTraversalDenied",
            ResultSeverities.ByName("Error"),
            "Path traversal denied: {path} escapes Root {root}")
    {
    }
}
