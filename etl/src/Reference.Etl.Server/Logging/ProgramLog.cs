using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Etl.Server.Logging;

/// <summary>
/// MessageLogging for ETL Server startup operations.
/// EventId range: 10000-10099
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class ProgramLog
{
    [MessageLogging(
        EventId = 10000,
        Level = LogLevel.Information,
        Message = "ETL Server starting")]
    public static partial IGenericMessage ServerStarting(ILogger logger);

    [MessageLogging(
        EventId = 10001,
        Level = LogLevel.Information,
        Message = "ETL Server started")]
    public static partial IGenericMessage ServerStarted(ILogger logger);

    [MessageLogging(
        EventId = 10002,
        Level = LogLevel.Information,
        Message = "ETL Server stopping")]
    public static partial IGenericMessage ServerStopping(ILogger logger);

    [MessageLogging(
        EventId = 10003,
        Level = LogLevel.Information,
        Message = "ETL Server stopped")]
    public static partial IGenericMessage ServerStopped(ILogger logger);

    [MessageLogging(
        EventId = 10020,
        Level = LogLevel.Debug,
        Message = "Starting service registration")]
    public static partial IGenericMessage ServiceRegistrationStarted(ILogger logger);

    [MessageLogging(
        EventId = 10021,
        Level = LogLevel.Information,
        Message = "Service registration completed, {count} services registered")]
    public static partial IGenericMessage ServiceRegistrationCompleted(ILogger logger, int count);

    [MessageLogging(
        EventId = 10030,
        Level = LogLevel.Information,
        Message = "Verifying database schema for table '{tableName}'")]
    public static partial IGenericMessage SchemaVerifying(ILogger logger, string tableName);

    [MessageLogging(
        EventId = 10031,
        Level = LogLevel.Information,
        Message = "Table '{tableName}' already exists, schema verification passed")]
    public static partial IGenericMessage SchemaExists(ILogger logger, string tableName);

    [MessageLogging(
        EventId = 10032,
        Level = LogLevel.Information,
        Message = "Creating table '{tableName}'")]
    public static partial IGenericMessage SchemaCreating(ILogger logger, string tableName);

    [MessageLogging(
        EventId = 10033,
        Level = LogLevel.Information,
        Message = "Table '{tableName}' created successfully")]
    public static partial IGenericMessage SchemaCreated(ILogger logger, string tableName);

    [MessageLogging(
        EventId = 10034,
        Level = LogLevel.Error,
        Message = "Failed to verify/create table '{tableName}'")]
    public static partial IGenericMessage SchemaVerificationFailed(ILogger logger, System.Exception exception, string tableName);

    [MessageLogging(
        EventId = 10035,
        Level = LogLevel.Information,
        Message = "Creating schema '{schemaName}'")]
    public static partial IGenericMessage SchemaNamespaceCreating(ILogger logger, string schemaName);

    [MessageLogging(
        EventId = 10036,
        Level = LogLevel.Information,
        Message = "Schema '{schemaName}' exists or was created")]
    public static partial IGenericMessage SchemaNamespaceReady(ILogger logger, string schemaName);
}
