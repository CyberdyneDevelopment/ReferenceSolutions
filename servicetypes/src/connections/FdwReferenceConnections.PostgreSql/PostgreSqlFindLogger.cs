using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.PostgreSql;

/// <summary>
/// MessageLogging for the connection's cross-field find (FDW-1361).
/// </summary>
[MessageLoggingTypeCode("POSTGRESQL")]
public static partial class PostgreSqlFindLogger
{
    // Why Debug: one line per find, with what it found; the gateway logs the command's completion above it.
    /// <summary>A find read its rows and the fields that matched in each.</summary>
    [MessageLogging(EventId = 12092, Level = LogLevel.Debug, Message = "Find in '{container}' read {rows} row(s)")]
    public static partial IGenericMessage FindRead(ILogger logger, string container, int rows);
}
