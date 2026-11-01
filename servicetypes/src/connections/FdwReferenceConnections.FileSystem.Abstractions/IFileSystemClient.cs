using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace FdwReferenceConnections.FileSystem.Abstractions;

/// <summary>
/// Typed primitive client for file system I/O.
/// All paths are relative to the connection's Root; the implementation
/// canonicalizes with <c>Path.GetFullPath</c> and enforces that the resolved
/// absolute path remains within Root before any I/O is performed.
/// </summary>
/// <remarks>
/// This is the surface connectors call directly per the §1.1 canary experiment.
/// Path traversal attempts return <c>PathTraversalDeniedCode</c> rather than
/// throwing an exception, preserving the fail-loud GenericResult contract.
/// </remarks>
public interface IFileSystemClient
{
    /// <summary>
    /// Reads the full text of the file at <paramref name="relativePath"/>.
    /// </summary>
    Task<IGenericResult<string>> ReadText(string relativePath, CancellationToken ct = default);

    /// <summary>
    /// Reads the text within the specified 1-based inclusive line range.
    /// </summary>
    Task<IGenericResult<string>> ReadText(string relativePath, RawTextLineRange lines, CancellationToken ct = default);

    /// <summary>
    /// Reads all bytes from the file at <paramref name="relativePath"/>.
    /// </summary>
    Task<IGenericResult<byte[]>> ReadBytes(string relativePath, CancellationToken ct = default);

    /// <summary>
    /// Reads a byte slice starting at <paramref name="offset"/> for <paramref name="length"/> bytes.
    /// </summary>
    Task<IGenericResult<byte[]>> ReadBytes(string relativePath, long offset, int length, CancellationToken ct = default);

    /// <summary>
    /// Returns whether the file at <paramref name="relativePath"/> exists.
    /// </summary>
    Task<IGenericResult<bool>> Exists(string relativePath, CancellationToken ct = default);

    /// <summary>
    /// Writes <paramref name="text"/> to the file at <paramref name="relativePath"/>,
    /// overwriting any existing content.
    /// Returns the number of characters written (informational — used by write connectors).
    /// </summary>
    Task<IGenericResult<int>> WriteText(string relativePath, string text, CancellationToken ct = default);

    /// <summary>
    /// Writes <paramref name="bytes"/> to the file at <paramref name="relativePath"/>,
    /// overwriting any existing content.
    /// Returns the number of bytes written.
    /// </summary>
    Task<IGenericResult<int>> WriteBytes(string relativePath, byte[] bytes, CancellationToken ct = default);

    /// <summary>
    /// Lists files relative to the connection's Root, optionally filtered by a relative prefix.
    /// Pass an empty string for the prefix to enumerate from Root.
    /// Recursive (returns paths from all subdirectories).
    /// </summary>
    Task<IGenericResult<IReadOnlyList<string>>> List(string relativePrefix, CancellationToken ct = default);
}
