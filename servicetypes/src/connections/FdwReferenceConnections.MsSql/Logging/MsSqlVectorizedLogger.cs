using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.MsSql.Logging;

/// <summary>
/// MessageLogging for the connection's vectorized commands: the rows nearest a vector (FDW-1354).
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlVectorizedLogger
{
    // Why Debug: one line per search, with what it found; the gateway logs the command's completion above it.
    /// <summary>A nearest search read its rows.</summary>
    [MessageLogging(EventId = 12090, Level = LogLevel.Debug, Message = "Nearest search of '{container}' read {rows} row(s)")]
    public static partial IGenericMessage NearestRead(ILogger logger, string container, int rows);

    // Why Error: a vectorized command reached the connection with a translator but no way to read its answer — a
    // command added to the family without its reading here.
    /// <summary>The connection has no reading for this vectorized command's answer.</summary>
    [MessageLogging(EventId = 91011, Level = LogLevel.Error, Message = "SQL Server has no reading for the answer of vectorized command '{commandType}'")]
    public static partial IGenericMessage CommandNotRead(ILogger logger, string commandType);
}
