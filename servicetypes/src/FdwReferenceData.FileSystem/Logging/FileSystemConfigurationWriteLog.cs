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

    /// <summary>Logs that a version-on-write configuration save is starting.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container being written.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace, Message = "FileSystem connection '{connectionName}' saving a new configuration version to container '{container}'")]
    public static partial IGenericMessage SaveStarting(ILogger logger, string connectionName, string container);

    /// <summary>Logs that a literal in-place configuration update is starting.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container being updated.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11011, Level = LogLevel.Trace, Message = "FileSystem connection '{connectionName}' updating configuration rows in container '{container}'")]
    public static partial IGenericMessage UpdateStarting(ILogger logger, string connectionName, string container);

    /// <summary>Logs that a soft-delete of a configuration record is starting.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container being soft-deleted from.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11012, Level = LogLevel.Trace, Message = "FileSystem connection '{connectionName}' soft-deleting a configuration record in container '{container}'")]
    public static partial IGenericMessage DeleteStarting(ILogger logger, string connectionName, string container);

    /// <summary>Logs that a prior current version was retired (IsCurrent flipped to false) before a save.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container whose prior current version was retired.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11013, Level = LogLevel.Trace, Message = "FileSystem connection '{connectionName}' retired the prior current version in container '{container}'")]
    public static partial IGenericMessage PriorVersionRetired(ILogger logger, string connectionName, string container);

    /// <summary>Logs that a configuration save completed.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container that was written.</param>
    /// <param name="rowCount">The number of rows written (1 for a save).</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11014, Level = LogLevel.Information, Message = "FileSystem connection '{connectionName}' saved {rowCount} configuration row(s) to container '{container}'")]
    public static partial IGenericMessage SaveCompleted(ILogger logger, string connectionName, string container, int rowCount);

    /// <summary>Logs that a configuration update completed.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container that was updated.</param>
    /// <param name="affectedRows">The number of rows mutated in place.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11015, Level = LogLevel.Information, Message = "FileSystem connection '{connectionName}' updated {affectedRows} configuration row(s) in container '{container}'")]
    public static partial IGenericMessage UpdateCompleted(ILogger logger, string connectionName, string container, int affectedRows);

    /// <summary>Logs that a configuration soft-delete completed.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container that was soft-deleted from.</param>
    /// <param name="affectedRows">The number of rows soft-deleted in place.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11016, Level = LogLevel.Information, Message = "FileSystem connection '{connectionName}' soft-deleted {affectedRows} configuration row(s) in container '{container}'")]
    public static partial IGenericMessage DeleteCompleted(ILogger logger, string connectionName, string container, int affectedRows);

    // ═══════════════════════════════════════════════════════════════════════════
    // Validation (category 2)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs that a command type reached the FileSystem connection that it does not handle.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="commandType">The unrecognized command type.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': unrecognized command type '{commandType}' — expected Query, Insert, ConfigurationSave, Update, or ConfigurationDelete")]
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

    /// <summary>Logs that no insertable columns were resolved for a ConfigurationSave command.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container the save targeted.</param>
    /// <param name="typeName">The record type name whose properties yielded no insertable columns.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21004, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': record type '{typeName}' has no insertable columns for container '{container}'")]
    public static partial IGenericMessage NoInsertableColumns(ILogger logger, string connectionName, string container, string typeName);

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

    /// <summary>Logs that the container declares no logical key, so version-on-write cannot proceed.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container that declares no logical key.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61010, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': container '{container}' declares no Logical or Foreign key — cannot resolve the version-on-write logical identity")]
    public static partial IGenericMessage LogicalKeyNotFound(ILogger logger, string connectionName, string container);

    /// <summary>Logs that the container declares no physical key, so a version RowId cannot be assigned.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container that declares no physical key.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61011, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': container '{container}' declares no Physical key — cannot assign a version RowId")]
    public static partial IGenericMessage PhysicalKeyNotFound(ILogger logger, string connectionName, string container);

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
