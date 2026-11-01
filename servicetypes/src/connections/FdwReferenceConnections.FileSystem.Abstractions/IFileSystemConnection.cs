using Fdw.Services.Connections.Abstractions;

namespace FdwReferenceConnections.FileSystem.Abstractions;

/// <summary>
/// Marker interface for a FileSystem connection.
/// Exposes the root directory path and the typed primitive client.
/// </summary>
public interface IFileSystemConnection : IGenericConnection
{
    /// <summary>
    /// Gets the canonicalized root directory path for this connection.
    /// All relative paths resolved by <see cref="IFileSystemClient"/> are
    /// anchored to this root.
    /// </summary>
    string Root { get; }

    /// <summary>
    /// Gets the typed primitive client for file I/O operations.
    /// Connectors call this directly per the §1.1 canary experiment.
    /// </summary>
    IFileSystemClient Client { get; }
}
