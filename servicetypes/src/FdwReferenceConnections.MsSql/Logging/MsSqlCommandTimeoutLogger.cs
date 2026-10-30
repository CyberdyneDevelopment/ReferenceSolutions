using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.MsSql.Logging;

/// <summary>
/// MessageLogging for a command that outran its connection's command timeout (FDW-3279).
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlCommandTimeoutLogger
{
    // Why Error: the command did not finish and its effect is unknown to the caller; the limit it was given is the timeout in the message.
    /// <summary>A command ran longer than the command timeout it was given, by its connection or by whoever sent it (FDW-3324).</summary>
    [MessageLogging(EventId = 81010, Level = LogLevel.Error,
        Message = "Command on SQL Server connection '{connectionName}' ran longer than its command timeout of {timeoutSeconds} seconds, set by {limitedBy}: {commandText}",
        ClientMessage = "The command ran longer than the command timeout of {timeoutSeconds} seconds set by {limitedBy}, on connection '{connectionName}'")]
    public static partial IGenericMessage CommandTimedOut(ILogger logger, string connectionName, int timeoutSeconds, string commandText, string limitedBy);
}
