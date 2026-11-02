using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceData.FileSystem.Logging;

/// <summary>
/// MessageLogging for the FileSystem configuration write path — the version-on-write CREATE
/// (<c>ConfigurationSave</c>), literal in-place <c>Update</c>, and soft-delete
/// (<c>ConfigurationDelete</c>) verbs handled by <c>FileSystemConfigurationWriter</c>.
/// EventIds draw from the <c>FILESYSTEM</c> TypeCode pool.
/// </summary>
[MessageLoggingTypeCode("FILESYSTEM")]
public static partial class FileSystemConfigurationWriteLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Trace / Information (category 1 — success/non-error)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs that a literal in-place configuration update is starting.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container being updated.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11011, Level = LogLevel.Trace, Message = "FileSystem connection '{connectionName}' updating configuration rows in container '{container}'")]
    public static partial IGenericMessage UpdateStarting(ILogger logger, string connectionName, string container);

    /// <summary>Logs that a configuration update completed.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container that was updated.</param>
    /// <param name="affectedRows">The number of rows mutated in place.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11015, Level = LogLevel.Information, Message = "FileSystem connection '{connectionName}' updated {affectedRows} configuration row(s) in container '{container}'")]
    public static partial IGenericMessage UpdateCompleted(ILogger logger, string connectionName, string container, int affectedRows);

    /// <summary>Traces a Delete data command starting against a container's file.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container whose rows are being removed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11019, Level = LogLevel.Trace, Message = "FileSystem connection '{connectionName}' removing rows from container '{container}'")]
    public static partial IGenericMessage DeleteRowsStarting(ILogger logger, string connectionName, string container);

    /// <summary>Reports a Delete data command that removed rows from a container's file.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container whose rows were removed.</param>
    /// <param name="affectedRows">The number of rows removed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11020, Level = LogLevel.Information, Message = "FileSystem connection '{connectionName}' removed {affectedRows} row(s) from container '{container}'")]
    public static partial IGenericMessage DeleteRowsCompleted(ILogger logger, string connectionName, string container, int affectedRows);

    /// <summary>Traces an Insert data command starting against a container's file.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container the rows are being added to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11021, Level = LogLevel.Trace, Message = "FileSystem connection '{connectionName}' adding rows to container '{container}'")]
    public static partial IGenericMessage InsertRowsStarting(ILogger logger, string connectionName, string container);

    /// <summary>Reports an Insert data command that added rows to a container's file.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container the rows were added to.</param>
    /// <param name="affectedRows">The number of rows added.</param>
    /// <param name="existingRows">The number of rows the file already held.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11022, Level = LogLevel.Information, Message = "FileSystem connection '{connectionName}' added {affectedRows} row(s) to container '{container}', which already held {existingRows}")]
    public static partial IGenericMessage InsertRowsCompleted(ILogger logger, string connectionName, string container, int affectedRows, int existingRows);

    /// <summary>Reports a Delete data command that flagged rows of a container's file instead of removing them.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container whose rows were flagged.</param>
    /// <param name="affectedRows">The number of rows flagged.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11025, Level = LogLevel.Information, Message = "FileSystem connection '{connectionName}' flagged {affectedRows} row(s) of container '{container}' instead of removing them")]
    public static partial IGenericMessage SoftDeleteRowsCompleted(ILogger logger, string connectionName, string container, int affectedRows);

    /// <summary>Logs that a soft delete's options cannot be used against a container.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container the soft delete targeted.</param>
    /// <param name="reason">Why the options were refused.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21008, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': the soft delete on container '{container}' is refused: {reason}")]
    public static partial IGenericMessage SoftDeleteRefused(ILogger logger, string connectionName, string container, string reason);

    /// <summary>Traces a Find data command starting against a container's file.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container whose rows are being searched.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11023, Level = LogLevel.Trace, Message = "FileSystem connection '{connectionName}' searching container '{container}'")]
    public static partial IGenericMessage FindStarting(ILogger logger, string connectionName, string container);

    /// <summary>Reports a Find data command that searched a container's file.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container whose rows were searched.</param>
    /// <param name="matches">The number of rows answered.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11024, Level = LogLevel.Information, Message = "FileSystem connection '{connectionName}' found {matches} row(s) in container '{container}'")]
    public static partial IGenericMessage FindCompleted(ILogger logger, string connectionName, string container, int matches);

    /// <summary>Logs that a Find command carried no search term.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container the search targeted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 20000, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': cannot search container '{container}' with an empty search term")]
    public static partial IGenericMessage FindSearchTermMissing(ILogger logger, string connectionName, string container);

    /// <summary>Logs that a Find command named a field the container does not declare.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container the search targeted.</param>
    /// <param name="field">The field the command named.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 20001, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': cannot search container '{container}' by field '{field}', which it does not declare")]
    public static partial IGenericMessage FindFieldNotDeclared(ILogger logger, string connectionName, string container, string field);

    /// <summary>Logs that a write command asked to return a column the container does not declare.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container the write targeted.</param>
    /// <param name="column">The column the command asked to return.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21005, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': cannot return column '{column}' from container '{container}', which does not declare it")]
    public static partial IGenericMessage ReturnColumnNotDeclared(ILogger logger, string connectionName, string container, string column);

    // ═══════════════════════════════════════════════════════════════════════════
    // Validation (category 2)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs that a command type reached the FileSystem connection that it does not handle.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="commandType">The unrecognized command type.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': unrecognized command type '{commandType}' — expected Query, Insert, Update, Delete, ConfigurationSave, or ConfigurationRetire")]
    public static partial IGenericMessage UnrecognizedCommandType(ILogger logger, string connectionName, string commandType);

    /// <summary>Logs that a write command carried no usable input data.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container the write targeted.</param>
    /// <param name="reason">The reason the input could not be resolved from the command.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21001, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': cannot write to container '{container}' — {reason}")]
    public static partial IGenericMessage WriteInputMissing(ILogger logger, string connectionName, string container, string reason);

    /// <summary>Logs that an Update command carried no filter to identify which rows to mutate.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container the update targeted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21002, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': Update on container '{container}' carried no filter — refusing to mutate every row")]
    public static partial IGenericMessage UpdateFilterMissing(ILogger logger, string connectionName, string container);

    /// <summary>Logs that no updatable columns were resolved for an Update command.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container the update targeted.</param>
    /// <param name="typeName">The record type name whose properties yielded no updatable columns.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21003, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': record type '{typeName}' has no updatable columns for container '{container}'")]
    public static partial IGenericMessage NoUpdatableColumns(ILogger logger, string connectionName, string container, string typeName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Missing (category 3)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs that a foreign-key parent row could not be resolved for a version-on-write save.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The child container being saved.</param>
    /// <param name="fkColumn">The child FK column that could not be resolved.</param>
    /// <param name="parentContainer">The parent container that was searched.</param>
    /// <param name="logicalId">The parent logical Id value that had no current row.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31010, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': container '{container}' FK column '{fkColumn}' has no current parent row in '{parentContainer}' for logical Id '{logicalId}'")]
    public static partial IGenericMessage ForeignKeyParentNotFound(ILogger logger, string connectionName, string container, string fkColumn, string parentContainer, string logicalId);

    /// <summary>Logs that the logical value needed to resolve a foreign key was absent from the record.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The child container being saved.</param>
    /// <param name="fkColumn">The child FK column that could not be resolved.</param>
    /// <param name="logicalColumn">The expected logical value column absent from the record.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31011, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': container '{container}' FK column '{fkColumn}' cannot be resolved — record carries no value for logical column '{logicalColumn}'")]
    public static partial IGenericMessage ForeignKeyLogicalValueMissing(ILogger logger, string connectionName, string container, string fkColumn, string logicalColumn);

    /// <summary>Logs that the parent container referenced by a foreign key could not be resolved in the container's path.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The child container being saved.</param>
    /// <param name="parentContainer">The parent container name that could not be resolved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31012, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': container '{container}' references parent container '{parentContainer}', which was not found in its path")]
    public static partial IGenericMessage ForeignKeyParentContainerNotResolved(ILogger logger, string connectionName, string container, string parentContainer);

    // ═══════════════════════════════════════════════════════════════════════════
    // Configuration (category 6)
    // ═══════════════════════════════════════════════════════════════════════════

    // ═══════════════════════════════════════════════════════════════════════════
    // Internal (category 9)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs that no PocoMapper is registered for the record type being written.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="typeName">The record type name with no registered PocoMapper.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': no PocoMapper registered for type '{typeName}' — ensure it has [GenerateMapper]")]
    public static partial IGenericMessage PocoMapperNotFound(ILogger logger, string connectionName, string typeName);

    /// <summary>Traces the start of resolving the owner a row hangs from.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container being written.</param>
    /// <param name="ownerContainer">The owner's container.</param>
    /// <param name="ownerId">The owner's Id.</param>
    /// <param name="domainContainer">The domain the owner is reached through, if it is an implementation.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11018, Level = LogLevel.Trace, Message = "FileSystem connection '{connectionName}': resolving owner '{ownerContainer}' ({ownerId}) of '{container}', through domain '{domainContainer}'")]
    public static partial IGenericMessage ResolvingOwner(ILogger logger, string connectionName, string container, string ownerContainer, System.Guid ownerId, string? domainContainer);

    /// <summary>Logs the configured Foreign key and owner RowId a row is written or retired under.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container being written.</param>
    /// <param name="ownerContainer">The owner's container.</param>
    /// <param name="fkColumn">The container's configured Foreign key column to the owner.</param>
    /// <param name="ownerRowId">The owner's current RowId.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11017, Level = LogLevel.Debug, Message = "FileSystem connection '{connectionName}': '{container}' hangs from '{ownerContainer}' RowId {ownerRowId} by column '{fkColumn}'")]
    public static partial IGenericMessage OwnerResolved(ILogger logger, string connectionName, string container, string ownerContainer, string fkColumn, long ownerRowId);

    /// <summary>Logs that a container configures no Foreign key to the owner a write named.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container being written.</param>
    /// <param name="ownerContainer">The owner's container.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21006, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': container '{container}' configures no Foreign key to its owner '{ownerContainer}'")]
    public static partial IGenericMessage OwnerForeignKeyNotConfigured(ILogger logger, string connectionName, string container, string ownerContainer);

    /// <summary>Logs that an owner or its domain declares no key of the type needed to find its row.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The owner or domain container.</param>
    /// <param name="keyType">The key type it does not declare.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21007, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': container '{container}' declares no {keyType} key, so the owner's row cannot be found")]
    public static partial IGenericMessage OwnerKeyNotConfigured(ILogger logger, string connectionName, string container, string keyType);

    /// <summary>Logs that no current owner row carries the owner Id a write named.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container being written.</param>
    /// <param name="ownerContainer">The container searched.</param>
    /// <param name="ownerId">The owner's Id.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31013, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': '{container}' names owner {ownerId}, but '{ownerContainer}' has no current row for it")]
    public static partial IGenericMessage OwnerRowNotFound(ILogger logger, string connectionName, string container, string ownerContainer, System.Guid ownerId);
}
