using System.Threading;
using System.Threading.Tasks;

namespace FdwReferenceConnections.FileSystem.Abstractions;

/// <summary>
/// Records what a file held before a unit of work overwrote it, so the unit can be undone.
/// </summary>
/// <remarks>
/// A file store has no native transaction, but the guarantee a configuration write needs from one is
/// narrow: a record is a row plus its children written by several statements, and a failure part way
/// through must not leave half of it behind. That is an undo log, not isolation.
/// <para>
/// The client captures a path's prior content before the first write that touches it, and the unit
/// restores every captured path if it rolls back. Writes are applied as they happen rather than
/// buffered, so a read inside the unit sees the unit's own earlier writes — which the configuration
/// providers rely on when they check whether a name already has a domain row.
/// </para>
/// <para>
/// What this does NOT provide is isolation from other writers. A concurrent process reading the same
/// store mid-unit sees the applied writes, and a concurrent writer can defeat the undo. That is a
/// real difference from a database transaction and is why it is stated here rather than implied.
/// </para>
/// </remarks>
public interface IFileSystemUndoLog
{
    /// <summary>
    /// Records the current content of a path before it is written, if it has not been recorded yet.
    /// </summary>
    /// <param name="relativePath">The path about to be written, relative to the connection root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes once the prior content has been captured.</returns>
    ValueTask Capture(string relativePath, CancellationToken cancellationToken = default);
}
