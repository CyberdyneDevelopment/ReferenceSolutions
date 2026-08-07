using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Scheduler.Server.Logging;

/// <summary>
/// MessageLogging for Scheduler Server startup operations.
/// EventId range: 11000-11099
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class StartupLog
{
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "Scheduler Server starting")]
    public static partial IGenericMessage ServerStarting(ILogger logger);

    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Scheduler Server started")]
    public static partial IGenericMessage ServerStarted(ILogger logger);

    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Information,
        Message = "Scheduler Server stopping")]
    public static partial IGenericMessage ServerStopping(ILogger logger);

    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "Scheduler Server stopped")]
    public static partial IGenericMessage ServerStopped(ILogger logger);

    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Debug,
        Message = "Connecting to ControlDb using connection '{connectionName}'")]
    public static partial IGenericMessage ControlDbConnecting(ILogger logger, string connectionName);

    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Information,
        Message = "Connected to ControlDb")]
    public static partial IGenericMessage ControlDbConnected(ILogger logger);

    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Error,
        Message = "Failed to connect to ControlDb: {error}")]
    public static partial IGenericMessage ControlDbConnectionFailed(ILogger logger, string error);

    [MessageLogging(
        EventId = 11020,
        Level = LogLevel.Debug,
        Message = "Starting service registration")]
    public static partial IGenericMessage ServiceRegistrationStarted(ILogger logger);

    [MessageLogging(
        EventId = 11021,
        Level = LogLevel.Information,
        Message = "Service registration completed, {count} services registered")]
    public static partial IGenericMessage ServiceRegistrationCompleted(ILogger logger, int count);

    [MessageLogging(
        EventId = 11030,
        Level = LogLevel.Information,
        Message = "Verifying database schema for table '{tableName}'")]
    public static partial IGenericMessage SchemaVerifying(ILogger logger, string tableName);

    [MessageLogging(
        EventId = 11031,
        Level = LogLevel.Information,
        Message = "Table '{tableName}' already exists, schema verification passed")]
    public static partial IGenericMessage SchemaExists(ILogger logger, string tableName);

    [MessageLogging(
        EventId = 11032,
        Level = LogLevel.Information,
        Message = "Creating table '{tableName}'")]
    public static partial IGenericMessage SchemaCreating(ILogger logger, string tableName);

    [MessageLogging(
        EventId = 11033,
        Level = LogLevel.Information,
        Message = "Table '{tableName}' created successfully")]
    public static partial IGenericMessage SchemaCreated(ILogger logger, string tableName);

    [MessageLogging(
        EventId = 11034,
        Level = LogLevel.Error,
        Message = "Failed to verify/create table '{tableName}'")]
    public static partial IGenericMessage SchemaVerificationFailed(ILogger logger, System.Exception exception, string tableName);

    [MessageLogging(
        EventId = 11035,
        Level = LogLevel.Information,
        Message = "Creating schema '{schemaName}'")]
    public static partial IGenericMessage SchemaNamespaceCreating(ILogger logger, string schemaName);

    [MessageLogging(
        EventId = 11036,
        Level = LogLevel.Information,
        Message = "Schema '{schemaName}' exists or was created")]
    public static partial IGenericMessage SchemaNamespaceReady(ILogger logger, string schemaName);

    [MessageLogging(
        EventId = 11040,
        Level = LogLevel.Information,
        Message = "Configuration loaded: {dataStoreCount} DataStores, {dataSetCount} DataSets")]
    public static partial IGenericMessage ConfigurationLoaded(ILogger logger, int dataStoreCount, int dataSetCount);

    [MessageLogging(
        EventId = 11041,
        Level = LogLevel.Critical,
        Message = "No scheduler configuration found in sched.Scheduler. Startup is aborting. " +
            "Add a row to sched.Scheduler in ConfigurationDb (DataStoreName, PathName, ScheduleContainerName) and restart")]
    public static partial IGenericMessage SchedulerConfigurationMissing(ILogger logger);

    [MessageLogging(
        EventId = 11042,
        Level = LogLevel.Critical,
        Message = "Scheduler configuration is incomplete: DataStoreName='{dataStoreName}', " +
            "PathName='{pathName}', ScheduleContainerName='{scheduleContainerName}'. All three are required")]
    public static partial IGenericMessage SchedulerConfigurationIncomplete(
        ILogger logger, string dataStoreName, string pathName, string scheduleContainerName);

    [MessageLogging(
        EventId = 11050,
        Level = LogLevel.Critical,
        Message = "Required configuration 'Kestrel:Endpoints:Http:Url' is missing. " +
            "Set this value in appsettings.[environment].json or via environment variable. " +
            "Application cannot start without a configured Kestrel endpoint")]
    public static partial IGenericMessage KestrelUrlMissing(ILogger logger);
}
