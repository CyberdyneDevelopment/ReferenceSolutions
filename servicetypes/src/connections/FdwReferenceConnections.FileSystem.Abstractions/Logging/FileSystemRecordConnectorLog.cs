using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.FileSystem.Abstractions.Logging;

/// <summary>
/// MessageLogging for the FileSystem config-driven record read/write seam (the record source/writer
/// path that reads and writes a configured file container through <c>RecordSourceTypes</c> /
/// <c>RecordWriterTypes</c>).
/// EventId range: 9580-9589.
/// </summary>
[MessageLoggingTypeCode("FS")]
public static partial class FileSystemRecordConnectorLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Trace (9580-9583)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the record connector is reading records from a configured file container.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container being read.</param>
    /// <param name="format">The configured record format.</param>
    /// <param name="relativePath">The relative file path being read.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace, Message = "FileSystem connection '{connectionName}' reading records from container '{container}' (format '{format}', file '{relativePath}')")]
    public static partial IGenericMessage ReadingRecords(ILogger logger, string connectionName, string container, string format, string relativePath);

    /// <summary>
    /// Logs that the record connector is writing records to a configured file container.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container being written.</param>
    /// <param name="format">The configured record format.</param>
    /// <param name="relativePath">The relative file path being written.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11011, Level = LogLevel.Trace, Message = "FileSystem connection '{connectionName}' writing records to container '{container}' (format '{format}', file '{relativePath}')")]
    public static partial IGenericMessage WritingRecords(ILogger logger, string connectionName, string container, string format, string relativePath);

    // ═══════════════════════════════════════════════════════════════════════════
    // Information (9584-9585)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the record connector finished reading records from a configured file container.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container that was read.</param>
    /// <param name="recordCount">The number of records that were read.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11012, Level = LogLevel.Information, Message = "FileSystem connection '{connectionName}' read {recordCount} records from container '{container}'")]
    public static partial IGenericMessage ReadRecordsCompleted(ILogger logger, string connectionName, string container, int recordCount);

    /// <summary>
    /// Logs that the record connector finished writing records to a configured file container.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container that was written.</param>
    /// <param name="recordCount">The number of records that were written.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11013, Level = LogLevel.Information, Message = "FileSystem connection '{connectionName}' wrote {recordCount} records to container '{container}'")]
    public static partial IGenericMessage WriteRecordsCompleted(ILogger logger, string connectionName, string container, int recordCount);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error (9586-9589)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the container carries no configured record format, so no record source/writer can be built.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container that is missing a format.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61001, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': container '{container}' has no configured record format")]
    public static partial IGenericMessage FormatNotConfigured(ILogger logger, string connectionName, string container);

    /// <summary>
    /// Logs that no record source/writer type is registered for the container's configured format.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="format">The configured format that has no registered source/writer type.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61002, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': no record source/writer registered for format '{format}'")]
    public static partial IGenericMessage FormatNotRegistered(ILogger logger, string connectionName, string format);

    /// <summary>
    /// Logs that the container's physical path is not a file path, so a file cannot be resolved.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container whose physical path is not a file path.</param>
    /// <param name="actualPathType">The actual physical path type found on the container.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21001, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': container '{container}' physical path is '{actualPathType}', not a file path")]
    public static partial IGenericMessage NotAFilePath(ILogger logger, string connectionName, string container, string actualPathType);

    /// <summary>
    /// Logs that the write command carried no records (or an unsupported record payload), so nothing
    /// could be written.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container the write targeted.</param>
    /// <param name="reason">The reason the records could not be resolved from the command.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21002, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': cannot write to container '{container}' — {reason}")]
    public static partial IGenericMessage WriteInputInvalid(ILogger logger, string connectionName, string container, string reason);

    /// <summary>
    /// Logs that a container child node is not an <c>IDataField</c>, so the record source/writer's
    /// field schema cannot be built.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the FileSystem connection.</param>
    /// <param name="container">The container whose child node is not an IDataField.</param>
    /// <param name="node">The offending child node's name.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21003, Level = LogLevel.Error, Message = "FileSystem connection '{connectionName}': container '{container}' child node '{node}' is not an IDataField")]
    public static partial IGenericMessage FieldNotIDataField(ILogger logger, string connectionName, string container, string node);
}
