using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace FdwReferenceConnections.FileSystem.Abstractions.Results.Codes;

/// <summary>
/// The FileSystemConnection is missing a required Root directory.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FileSystemResultCodes), "RootNotConfigured", RestrictToCurrentCompilation = true)]
public sealed class RootNotConfiguredCode : FileSystemResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RootNotConfiguredCode"/> class.
    /// </summary>
    public RootNotConfiguredCode()
        : base(
            60000,
            "RootNotConfigured",
            ResultSeverities.ByName("Error"),
            "FileSystemConnection {connection} missing required Root")
    {
    }
}
