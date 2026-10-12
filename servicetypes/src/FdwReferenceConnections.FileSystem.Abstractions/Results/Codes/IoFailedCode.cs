using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceConnections.FileSystem.Abstractions.Results.Codes;

/// <summary>
/// A general I/O failure occurred during a file operation.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FileSystemResultCodes), "IoFailed", RestrictToCurrentCompilation = true)]
public sealed class IoFailedCode : FileSystemResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IoFailedCode"/> class.
    /// </summary>
    public IoFailedCode()
        : base(
            71000,
            "IoFailed",
            ResultSeverities.ByName("Error"),
            "I/O failure: {message}",
            isRetryable: true)
    {
    }
}
