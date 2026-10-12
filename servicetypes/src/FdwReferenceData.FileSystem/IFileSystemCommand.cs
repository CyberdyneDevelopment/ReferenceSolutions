namespace FdwReferenceData.FileSystem;

/// <summary>
/// Marker interface for the native FileSystem command type used by
/// <c>ConnectionBase&lt;TCommand, ...&gt;</c>.
/// </summary>
/// <remarks>
/// No concrete FileSystem DataGateway commands ship in 1.1.1 — this marker exists only to
/// satisfy the <c>ConnectionBase</c> generic constraint and disambiguate the
/// <c>Execute&lt;T&gt;(TCommand, IStorageContainer, CancellationToken)</c> overload
/// from <c>Execute&lt;T&gt;(IDataCommand, IStorageContainer, CancellationToken)</c>
/// when <c>TCommand = IDataCommand</c> would make them identical.
/// In 1.2.0, concrete file commands (e.g., <c>FileReadTextCommand</c>) will implement this.
/// </remarks>
public interface IFileSystemCommand
{
}
