using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging;
using FdwReferenceConnections.FileSystem.Abstractions;
using FileSystemLog = FdwReferenceConnections.FileSystem.Abstractions.Logging.FileSystemConnectionLog;

namespace FdwReferenceConnections.FileSystem;

/// <summary>
/// A unit of work over a file-backed store: writes are applied as they happen and every file they
/// overwrote is put back if the unit rolls back.
/// </summary>
/// <remarks>
/// A file store has no native transaction, but the guarantee the configuration write path needs is
/// narrow. A configuration record is a domain row plus an implementation row plus its children,
/// written by several statements through two providers; written one at a time, a failure part way
/// through leaves a record half-written that can never be composed back. Undoing the writes that did
/// land is enough to prevent that, and is what this provides.
/// <para>
/// Writes are applied rather than buffered so that a read inside the unit sees the unit's own earlier
/// writes — which the providers depend on when they read back a record mid-operation to decide what a
/// delete must retire or whether a name already has a domain row.
/// </para>
/// <para>
/// This is NOT isolation. Another process reading the store mid-unit sees writes that may still be
/// rolled back, and another process writing the same paths can defeat the undo. A file store cannot
/// offer more than this without a lock manager, and pretending otherwise would be worse than saying
/// so: the configuration store is written by one process at a time.
/// </para>
/// </remarks>
public sealed class FileSystemConnectionTransaction : IDataConnectionTransaction, IFileSystemUndoLog
{
    private readonly FileSystemConnection _connection;
    private readonly FileSystemClient _client;
    private readonly string _root;
    private readonly string _connectionName;
    private readonly ILogger _logger;

    // The prior content of each path this unit has written, by relative path. A null value records
    // that the file did not exist, so undoing it means deleting it rather than restoring bytes.
    private readonly Dictionary<string, byte[]?> _captured = new(StringComparer.Ordinal);

    internal FileSystemConnectionTransaction(
        FileSystemConnection connection,
        FileSystemClient client,
        string root,
        string connectionName,
        ILogger logger)
    {
        _connection = connection;
        _client = client;
        _root = root;
        _connectionName = connectionName;
        _logger = logger;
        IsActive = true;
    }

    /// <inheritdoc />
    public bool IsActive { get; private set; }

    /// <inheritdoc />
    public async ValueTask Capture(string relativePath, CancellationToken cancellationToken = default)
    {
        if (_captured.ContainsKey(relativePath))
        {
            // Only the content as the unit FOUND it can restore the store; a later capture would
            // record this unit's own earlier write and undo to a state that never existed before it.
            return;
        }

        var full = Path.Combine(_root, relativePath);
        _captured[relativePath] = File.Exists(full)
            ? await File.ReadAllBytesAsync(full, cancellationToken).ConfigureAwait(false)
            : null;
    }

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(
        IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default)
        => IsActive
            ? _connection.Execute<T>(command, container, cancellationToken)
            : Task.FromResult(GenericResult<T>.Failure(
                FileSystemLog.UnitOfWorkNoLongerActive(_logger, _connectionName)));

    /// <inheritdoc />
    public Task<IGenericResult> Execute(
        IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default)
        => IsActive
            ? _connection.Execute(command, container, cancellationToken)
            : Task.FromResult<IGenericResult>(GenericResult.Failure(
                FileSystemLog.UnitOfWorkNoLongerActive(_logger, _connectionName)));

    /// <inheritdoc />
    public Task<IGenericResult> Commit(CancellationToken cancellationToken = default)
    {
        if (!IsActive)
        {
            return Task.FromResult<IGenericResult>(GenericResult.Failure(
                FileSystemLog.UnitOfWorkNoLongerActive(_logger, _connectionName)));
        }

        // Why committing writes nothing: the writes are already on disk. Committing only means
        // giving up the ability to undo them.
        FileSystemLog.UnitOfWorkCommitted(_logger, _connectionName, _captured.Count);
        End();
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }

    /// <inheritdoc />
    public Task<IGenericResult> Rollback(CancellationToken cancellationToken = default)
    {
        if (!IsActive)
        {
            return Task.FromResult<IGenericResult>(GenericResult.Failure(
                FileSystemLog.UnitOfWorkNoLongerActive(_logger, _connectionName)));
        }

        var restored = 0;
        IGenericMessageHolder? firstFailure = null;

        foreach (var entry in _captured)
        {
            var full = Path.Combine(_root, entry.Key);
            try
            {
                if (entry.Value is null)
                {
                    if (File.Exists(full)) File.Delete(full);
                }
                else
                {
                    File.WriteAllBytes(full, entry.Value);
                }

                restored++;
            }
            catch (IOException ex)
            {
                // Every remaining path is still attempted: stopping at the first failure would leave
                // more of the store wrong than carrying on does.
                firstFailure ??= new IGenericMessageHolder(
                    FileSystemLog.UnitOfWorkRestoreFailed(_logger, _connectionName, entry.Key, ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                firstFailure ??= new IGenericMessageHolder(
                    FileSystemLog.UnitOfWorkRestoreFailed(_logger, _connectionName, entry.Key, ex.Message));
            }
        }

        FileSystemLog.UnitOfWorkRolledBack(_logger, _connectionName, restored);
        End();

        return Task.FromResult(firstFailure is null
            ? GenericResult.Success()
            : GenericResult.Failure(firstFailure.Message));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // An undisposed unit that was never committed rolls back, so an early return or a thrown
        // exception cannot silently leave half a record behind.
        if (IsActive)
        {
            await Rollback(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private void End()
    {
        IsActive = false;
        _captured.Clear();
        _client.DetachUndoLog();
    }

    // Holds the first restore failure so the loop can carry on without the message being overwritten.
    private sealed class IGenericMessageHolder(Fdw.Messages.IGenericMessage message)
    {
        public Fdw.Messages.IGenericMessage Message { get; } = message;
    }
}
