using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.PostgreSql.Logging;

/// <summary>
/// MessageLogging for a PostgreSQL connection whose SslMode names no PostgreSqlSslModes option (FDW-4153).
/// </summary>
[MessageLoggingTypeCode("PGSQL")]
public static partial class PostgreSqlSslModeLogger
{
    /// <summary>A connection states an SslMode that is none of the modes, or states none; no mode is taken in its place.</summary>
    [MessageLogging(EventId = 21030, Level = LogLevel.Error,
        Message = "PostgreSQL connection '{connectionName}' states SslMode '{sslMode}', which is not one of {modes}",
        ClientMessage = "Connection '{connectionName}' states SslMode '{sslMode}', which is not one of {modes}")]
    public static partial IGenericMessage SslModeUnknown(ILogger logger, string connectionName, string sslMode, string modes);
}
