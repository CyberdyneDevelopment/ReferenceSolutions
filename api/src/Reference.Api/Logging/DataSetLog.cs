using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging definitions for DataSet management endpoints.
/// EventId range: 5400-5499
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class DataSetLog
{
    // List operations (5400-5409)
    [MessageLogging(EventId = 5400, Level = LogLevel.Information, Message = "Listing DataSets")]
    public static partial IGenericMessage ListingDataSets(ILogger logger);

    [MessageLogging(EventId = 5401, Level = LogLevel.Information, Message = "Found {count} DataSets")]
    public static partial IGenericMessage DataSetsFound(ILogger logger, int count);

    [MessageLogging(EventId = 5402, Level = LogLevel.Error, Message = "Failed to list DataSets: {message}")]
    public static partial IGenericMessage ListDataSetsFailed(ILogger logger, string message);

    // Read operations (5410-5419)
    [MessageLogging(EventId = 5410, Level = LogLevel.Information, Message = "Fetching DataSet '{name}'")]
    public static partial IGenericMessage FetchingDataSet(ILogger logger, string name);

    [MessageLogging(EventId = 5411, Level = LogLevel.Information, Message = "DataSet '{name}' retrieved with {fieldCount} fields and {sourceCount} sources")]
    public static partial IGenericMessage DataSetRetrieved(ILogger logger, string name, int fieldCount, int sourceCount);

    [MessageLogging(EventId = 5412, Level = LogLevel.Warning, Message = "DataSet '{name}' not found")]
    public static partial IGenericMessage DataSetNotFound(ILogger logger, string name);

    [MessageLogging(EventId = 5413, Level = LogLevel.Error, Message = "Failed to fetch DataSet '{name}': {message}")]
    public static partial IGenericMessage DataSetFetchFailed(ILogger logger, string name, string message);

    // Create operations (5420-5429)
    [MessageLogging(EventId = 5420, Level = LogLevel.Information, Message = "Creating DataSet '{name}'")]
    public static partial IGenericMessage CreatingDataSet(ILogger logger, string name);

    [MessageLogging(EventId = 5421, Level = LogLevel.Information, Message = "DataSet '{name}' created with {fieldCount} fields")]
    public static partial IGenericMessage DataSetCreated(ILogger logger, string name, int fieldCount);

    [MessageLogging(EventId = 5422, Level = LogLevel.Error, Message = "Failed to create DataSet '{name}': {message}")]
    public static partial IGenericMessage DataSetCreateFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 5423, Level = LogLevel.Warning, Message = "DataSet '{name}' already exists")]
    public static partial IGenericMessage DataSetAlreadyExists(ILogger logger, string name);

    // Update operations (5430-5439)
    [MessageLogging(EventId = 5430, Level = LogLevel.Information, Message = "Updating DataSet '{name}'")]
    public static partial IGenericMessage UpdatingDataSet(ILogger logger, string name);

    [MessageLogging(EventId = 5431, Level = LogLevel.Information, Message = "DataSet '{name}' updated successfully")]
    public static partial IGenericMessage DataSetUpdated(ILogger logger, string name);

    [MessageLogging(EventId = 5432, Level = LogLevel.Error, Message = "Failed to update DataSet '{name}': {message}")]
    public static partial IGenericMessage DataSetUpdateFailed(ILogger logger, string name, string message);

    // Delete operations (5440-5449)
    [MessageLogging(EventId = 5440, Level = LogLevel.Information, Message = "Deleting DataSet '{name}'")]
    public static partial IGenericMessage DeletingDataSet(ILogger logger, string name);

    [MessageLogging(EventId = 5441, Level = LogLevel.Information, Message = "DataSet '{name}' deleted successfully")]
    public static partial IGenericMessage DataSetDeleted(ILogger logger, string name);

    [MessageLogging(EventId = 5442, Level = LogLevel.Error, Message = "Failed to delete DataSet '{name}': {message}")]
    public static partial IGenericMessage DataSetDeleteFailed(ILogger logger, string name, string message);

    // Field operations (5450-5459)
    [MessageLogging(EventId = 5450, Level = LogLevel.Information, Message = "Fetching fields for DataSet '{name}'")]
    public static partial IGenericMessage FetchingFields(ILogger logger, string name);

    [MessageLogging(EventId = 5451, Level = LogLevel.Information, Message = "Found {count} fields for DataSet '{name}'")]
    public static partial IGenericMessage FieldsRetrieved(ILogger logger, string name, int count);

    [MessageLogging(EventId = 5452, Level = LogLevel.Warning, Message = "Failed to create field '{fieldName}' for DataSet '{dataSetName}': {message}")]
    public static partial IGenericMessage FieldCreateFailed(ILogger logger, string fieldName, string dataSetName, string message);

    // Source operations (5460-5469)
    [MessageLogging(EventId = 5460, Level = LogLevel.Information, Message = "Fetching sources for DataSet '{name}'")]
    public static partial IGenericMessage FetchingSources(ILogger logger, string name);

    [MessageLogging(EventId = 5461, Level = LogLevel.Information, Message = "Found {count} sources for DataSet '{name}'")]
    public static partial IGenericMessage SourcesRetrieved(ILogger logger, string name, int count);
}
