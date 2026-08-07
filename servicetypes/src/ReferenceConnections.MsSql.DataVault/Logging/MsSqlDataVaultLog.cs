using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace ReferenceConnections.MsSql.DataVault.Logging;

/// <summary>
/// MessageLogging for the SQL Server data-vault tier.
/// EventId range: 4296-4298.
/// Never logs SQL text-with-values, parameters, or any secret material — only the operation and outcome.
/// </summary>
[MessageLoggingTypeCode("DATAVAULTMSSQL")]
public static partial class MsSqlDataVaultLog
{
    /// <summary>
    /// Logs that a vault resolved a connection that is not a SQL Server connection.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="vaultName">The name of the vault whose connection is not a SQL Server connection.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 41000, Level = LogLevel.Error,
        Message = "Vault '{vaultName}' resolved a connection that is not a SQL Server connection")]
    public static partial IGenericMessage ConnectionNotMsSql(ILogger logger, string vaultName);

    /// <summary>
    /// Logs that a vault query operation failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the query to fail.</param>
    /// <param name="vaultName">The name of the vault whose query failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "Vault '{vaultName}' query failed")]
    public static partial IGenericMessage QueryFailed(ILogger logger, Exception exception, string vaultName);

    /// <summary>
    /// Logs that a vault non-query operation failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the non-query to fail.</param>
    /// <param name="vaultName">The name of the vault whose non-query failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "Vault '{vaultName}' non-query failed")]
    public static partial IGenericMessage NonQueryFailed(ILogger logger, Exception exception, string vaultName);
}
