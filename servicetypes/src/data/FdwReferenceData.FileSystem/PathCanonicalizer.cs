using System;
using System.IO;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FileSystemLog = FdwReferenceConnections.FileSystem.Abstractions.Logging.FileSystemConnectionLog;

namespace FdwReferenceData.FileSystem;

/// <summary>
/// Resolves relative paths against a connection Root and enforces sandbox isolation.
/// </summary>
/// <remarks>
/// Path resolution uses <c>Path.GetFullPath</c> to canonicalize separators and
/// eliminate <c>../</c> segments. On Linux the comparison is case-sensitive
/// (<see cref="StringComparison.Ordinal"/>); on Windows it is case-insensitive
/// (<see cref="StringComparison.OrdinalIgnoreCase"/>). This mirrors OS-level
/// file system semantics.
/// </remarks>
public static class PathCanonicalizer
{
    // Why: Runtime OS check instead of compile-time constant so the same binary works
    // correctly on both Linux (case-sensitive) and Windows (case-insensitive) hosts.
    private static readonly StringComparison _pathComparison =
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    /// <summary>
    /// Resolves <paramref name="relativePath"/> against <paramref name="canonicalRoot"/>
    /// and returns the absolute path, or a failure result if the resolved path escapes Root.
    /// </summary>
    /// <param name="canonicalRoot">
    /// The already-canonicalized Root (produced by <c>Path.GetFullPath(config.Root)</c>
    /// at connection creation time).
    /// </param>
    /// <param name="relativePath">The caller-supplied relative path.</param>
    /// <param name="connectionName">Connection name for error logging.</param>
    /// <param name="logger">Logger; falls back to NullLogger if null.</param>
    public static IGenericResult<string> Resolve(
        string canonicalRoot,
        string relativePath,
        string connectionName,
        ILogger? logger)
    {
        ILogger log = logger ?? NullLogger.Instance;

        string resolved;
        try
        {
            // Why: Combine then GetFullPath collapses all .. and . segments.
            resolved = Path.GetFullPath(Path.Combine(canonicalRoot, relativePath));
        }
        catch (Exception ex)
        {
            return GenericResult<string>.Failure(
                FileSystemLog.IoFailed(log, ex, connectionName, relativePath, ex.Message));
        }

        // Why: Ensure Root ends with separator so "root/secret" isn't confused with "root/sec".
        string rootWithSep = canonicalRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), _pathComparison)
            ? canonicalRoot
            : canonicalRoot + Path.DirectorySeparatorChar;

        if (!resolved.StartsWith(rootWithSep, _pathComparison) &&
            !string.Equals(resolved, canonicalRoot, _pathComparison))
        {
            return GenericResult<string>.Failure(
                FileSystemLog.PathTraversalDenied(log, connectionName, relativePath, canonicalRoot));
        }

        return GenericResult<string>.Success(resolved);
    }
}
