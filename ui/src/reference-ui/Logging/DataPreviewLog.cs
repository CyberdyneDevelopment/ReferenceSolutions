using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Ui.Logging;

/// <summary>
/// MessageLogging for Data Preview page operations.
/// EventId range: 1810-1819
/// </summary>
public static partial class DataPreviewLog
{
    [MessageLogging(
        EventId = 1810,
        Level = LogLevel.Error,
        Message = "Failed to load connections for data preview")]
    public static partial IGenericMessage LoadConnectionsFailed(
        ILogger logger,
        Exception exception);

    [MessageLogging(
        EventId = 1811,
        Level = LogLevel.Error,
        Message = "Failed to load schema for connection '{connectionName}'")]
    public static partial IGenericMessage LoadSchemaFailed(
        ILogger logger,
        Exception exception,
        string connectionName);

    [MessageLogging(
        EventId = 1812,
        Level = LogLevel.Error,
        Message = "Failed to load DataSets for preview")]
    public static partial IGenericMessage LoadDataSetsFailed(
        ILogger logger,
        Exception exception);

    [MessageLogging(
        EventId = 1813,
        Level = LogLevel.Error,
        Message = "Failed to load DataSet '{dataSetName}' details")]
    public static partial IGenericMessage LoadDataSetDetailFailed(
        ILogger logger,
        Exception exception,
        string dataSetName);

    [MessageLogging(
        EventId = 1814,
        Level = LogLevel.Information,
        Message = "Data preview returned {rowCount} rows from {schema}.{table}")]
    public static partial IGenericMessage PreviewReturned(
        ILogger logger,
        int rowCount,
        string schema,
        string table);

    [MessageLogging(
        EventId = 1815,
        Level = LogLevel.Error,
        Message = "Data preview failed for {schema}.{table}")]
    public static partial IGenericMessage PreviewFailed(
        ILogger logger,
        Exception exception,
        string schema,
        string table);

    [MessageLogging(
        EventId = 1816,
        Level = LogLevel.Information,
        Message = "Data preview returned {rowCount} rows from DataSet '{dataSetName}'")]
    public static partial IGenericMessage DataSetPreviewReturned(
        ILogger logger,
        int rowCount,
        string dataSetName);

    [MessageLogging(
        EventId = 1817,
        Level = LogLevel.Error,
        Message = "Data preview failed for DataSet '{dataSetName}'")]
    public static partial IGenericMessage DataSetPreviewFailed(
        ILogger logger,
        Exception exception,
        string dataSetName);
}
