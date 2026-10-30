using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.PostgreSql.Logging;

/// <summary>
/// MessageLogging for a command that outran its connection's command timeout (FDW-3279).
/// </summary>
[MessageLoggingTypeCode("PGSQL")]
public static partial class PostgreSqlCommandTimeoutLogger
{
    // Why Error: the command did not finish and its effect is unknown to the caller; the connection's stored timeout is the limit it was given.
    /// <summary>A command ran longer than its connection's configured command timeout.</summary>
    [MessageLogging(EventId = 81010, Level = LogLevel.Error,
        Message = "Command on PostgreSQL connection '{connectionName}' ran longer than the connection's command timeout of {timeoutSeconds} seconds: {commandText}",
        ClientMessage = "The command ran longer than the command timeout of connection '{connectionName}', which is {timeoutSeconds} seconds")]
    public static partial IGenericMessage CommandTimedOut(ILogger logger, string connectionName, int timeoutSeconds, string commandText);
}
