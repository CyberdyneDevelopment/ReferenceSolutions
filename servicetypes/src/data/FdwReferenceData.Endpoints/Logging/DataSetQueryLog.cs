using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints.Logging;

/// <summary>
/// MessageLogging definitions for DataSet query endpoint.
/// EventId range: 5470-5489
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class DataSetQueryLog
{
    [MessageLogging(EventId = 5470, Level = LogLevel.Trace, Message = "Querying DataSet '{dataSetName}' with skip={skip}, take={take}")]
    public static partial IGenericMessage QueryingDataSet(ILogger logger, string dataSetName, int skip, int take);

    [MessageLogging(EventId = 5471, Level = LogLevel.Trace, Message = "Resolving DataSet '{dataSetName}' configuration")]
    public static partial IGenericMessage ResolvingDataSet(ILogger logger, string dataSetName);

    [MessageLogging(EventId = 5472, Level = LogLevel.Warning, Message = "DataSet '{dataSetName}' not found")]
    public static partial IGenericMessage DataSetNotFound(ILogger logger, string dataSetName);

    [MessageLogging(EventId = 5473, Level = LogLevel.Trace, Message = "Resolved {fieldCount} fields for DataSet '{dataSetName}'")]
    public static partial IGenericMessage FieldsResolved(ILogger logger, string dataSetName, int fieldCount);

    [MessageLogging(EventId = 5474, Level = LogLevel.Trace, Message = "Applying {filterCount} query filters for DataSet '{dataSetName}'")]
    public static partial IGenericMessage ApplyingFilters(ILogger logger, string dataSetName, int filterCount);

    [MessageLogging(EventId = 5478, Level = LogLevel.Information, Message = "DataSet '{dataSetName}' query returned {rowCount} rows (hasMore={hasMore})")]
    public static partial IGenericMessage QueryCompleted(ILogger logger, string dataSetName, int rowCount, bool hasMore);

    [MessageLogging(EventId = 5480, Level = LogLevel.Warning, Message = "Query filter field '{fieldName}' not found in DataSet '{dataSetName}', ignoring")]
    public static partial IGenericMessage UnknownFilterField(ILogger logger, string dataSetName, string fieldName);

    [MessageLogging(EventId = 5481, Level = LogLevel.Error, Message = "DataSet '{dataSetName}' stream failed while it was being written; the response is aborted so it cannot pass for a whole one")]
    public static partial IGenericMessage StreamAborted(ILogger logger, Exception ex, string dataSetName);
}
