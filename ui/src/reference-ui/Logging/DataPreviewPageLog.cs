using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Ui.Logging;

/// <summary>
/// Structured logging for DataPreviewPageProvider.
/// EventId range: 1750-1759
/// </summary>
public static partial class DataPreviewPageLog
{
    [MessageLogging(EventId = 1750, Level = LogLevel.Trace, Message = "Connection changed to '{connection}' — triggering schema discovery")]
    public static partial IGenericMessage ConnectionChanged(ILogger logger, string connection);

    [MessageLogging(EventId = 1751, Level = LogLevel.Trace, Message = "Preview executed for connection '{connection}', schema '{schema}', table '{table}'")]
    public static partial IGenericMessage PreviewExecuted(ILogger logger, string connection, string schema, string table);

    [MessageLogging(EventId = 1752, Level = LogLevel.Trace, Message = "Exporting {rowCount} rows as CSV")]
    public static partial IGenericMessage ExportingCsv(ILogger logger, int rowCount);

    [MessageLogging(EventId = 1753, Level = LogLevel.Trace, Message = "Session state restored for data preview")]
    public static partial IGenericMessage SessionStateRestored(ILogger logger);
}
