using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceHosts.SqlServerEndpoint.Logging;

/// <summary>
/// Structured log messages for the SQL Server endpoint host option.
/// </summary>
public static partial class SqlServerEndpointHostLog
{
    /// <summary>The host service was sent a command.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The command's type.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62401, Level = LogLevel.Error,
        Message = "The SQL Server endpoint host service carries settings and executes nothing; it was sent a '{commandType}'")]
    public static partial IGenericMessage ServiceExecutesNothing(ILogger logger, string commandType);

    /// <summary>The endpoint's row is missing or is not a host row.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The row's name.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62402, Level = LogLevel.Error,
        Message = "No SqlServerEndpoint row named '{name}' with a host implementation is configured; where and as what this host listens for SQL Server clients is configuration, not a default")]
    public static partial IGenericMessage RowMissing(ILogger logger, string name);

    /// <summary>A setting on the endpoint's row cannot be used.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="setting">The setting.</param>
    /// <param name="reason">Why it cannot be used.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62403, Level = LogLevel.Error,
        Message = "The SQL Server endpoint setting {setting} cannot be used: {reason}")]
    public static partial IGenericMessage SettingInvalid(ILogger logger, string setting, string reason);

    /// <summary>A secret the endpoint names cannot be read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="secretName">The secret's name.</param>
    /// <param name="secretManagerName">The secret manager it was read from.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62404, Level = LogLevel.Error,
        Message = "The SQL Server endpoint secret '{secretName}' cannot be read from secret manager '{secretManagerName}'")]
    public static partial IGenericMessage SecretUnreadable(ILogger logger, string secretName, string secretManagerName);

    /// <summary>The endpoint's options were read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="endpoint">The address and port listened on.</param>
    /// <param name="serverName">The server name presented.</param>
    /// <param name="loginFlowName">The flow a SQL login runs.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 62405, Level = LogLevel.Information,
        Message = "The SQL Server endpoint will listen on {endpoint} as '{serverName}', signing logins in through flow '{loginFlowName}'")]
    public static partial IGenericMessage OptionsRead(ILogger logger, string endpoint, string serverName, string loginFlowName);
}
