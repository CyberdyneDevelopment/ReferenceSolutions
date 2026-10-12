using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// MessageLogging for the DataSet export endpoint.
/// EventId range: 8600-8619.
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class ExportDataSetLog
{
    /// <summary>Logs that the requested data set does not exist.</summary>
    [MessageLogging(EventId = 8600, Level = LogLevel.Warning, Message = "Data set '{name}' was not found, so there is nothing to export")]
    public static partial IGenericMessage DataSetNotFound(ILogger logger, string name);
}
