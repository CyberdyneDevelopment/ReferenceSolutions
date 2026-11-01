using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace FdwReferenceConnections.MsSql;

/// <summary>
/// MessageLogging for the MsSql implementation of the Connection domain.
/// EventId range: 11027-11029.
/// </summary>
public static partial class MsSqlConnectionProviderLogger
{
    /// <summary>Traces entry to the implementation provider, before anything is resolved.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection being built.</param>
    /// <returns>The logged message.</returns>
    [MessageLogging(
        EventId = 11027,
        Level = LogLevel.Trace,
        Message = "MsSql connection '{connectionName}': resolving before build")]
    public static partial IGenericMessage TraceResolving(
        ILogger<MsSqlConnectionProvider> logger,
        string connectionName);

    /// <summary>Traces that the authentication type declared no secret, so nothing is fetched.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection being built.</param>
    /// <param name="authenticationType">The authentication type that declared nothing.</param>
    /// <returns>The logged message.</returns>
    [MessageLogging(
        EventId = 11028,
        Level = LogLevel.Trace,
        Message = "MsSql connection '{connectionName}': '{authenticationType}' needs no secret")]
    public static partial IGenericMessage TraceNoSecretRequired(
        ILogger<MsSqlConnectionProvider> logger,
        string connectionName,
        string authenticationType);

    /// <summary>Traces the secret this connection's authentication type asked for, before fetching it.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection being built.</param>
    /// <param name="managerName">The secret manager the authentication type named.</param>
    /// <returns>The logged message.</returns>
    /// <remarks>
    /// The manager and the connection are named; the key and the value are not. Which store was read
    /// is what an operator needs to follow a failure, and naming the key narrows the search for
    /// whoever holds the store.
    /// </remarks>
    [MessageLogging(
        EventId = 11029,
        Level = LogLevel.Trace,
        Message = "MsSql connection '{connectionName}': reading its secret from '{managerName}'")]
    public static partial IGenericMessage TraceResolvingSecret(
        ILogger<MsSqlConnectionProvider> logger,
        string connectionName,
        string managerName);

    /// <summary>Logs that the resolved secret manager's own name does not match the one requested.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionName">The connection being built.</param>
    /// <param name="declared">The manager name the connection's authentication type named.</param>
    /// <param name="resolved">The name the returned manager actually carries.</param>
    /// <returns>The logged message.</returns>
    /// <remarks>
    /// The provider is asked for a manager by name; trusting whatever comes back without checking its
    /// own name would let a misconfigured or malicious provider silently substitute a different
    /// secret store for the one the connection declared.
    /// </remarks>
    [MessageLogging(
        EventId = 21100,
        Level = LogLevel.Error,
        Message = "Connection '{connectionName}' declares secret manager '{declared}' but the resolved manager's own name is '{resolved}' — refusing to read a secret from a store this connection did not name")]
    public static partial IGenericMessage SecretManagerMismatch(
        ILogger<MsSqlConnectionProvider> logger,
        string connectionName,
        string declared,
        string resolved);
}
