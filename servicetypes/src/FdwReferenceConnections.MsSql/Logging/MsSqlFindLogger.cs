using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.MsSql.Logging;

/// <summary>
/// MessageLogging for the connection's cross-field find (FDW-1361).
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlFindLogger
{
    // Why Debug: one line per find, with what it found; the gateway logs the command's completion above it.
    /// <summary>A find read its rows and the fields that matched in each.</summary>
    [MessageLogging(EventId = 12091, Level = LogLevel.Debug, Message = "Find in '{container}' read {rows} row(s)")]
    public static partial IGenericMessage FindRead(ILogger logger, string container, int rows);
}
