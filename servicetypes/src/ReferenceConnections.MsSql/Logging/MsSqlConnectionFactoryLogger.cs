using Fdw.Configuration;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace ReferenceConnections.MsSql.Logging;

/// <summary>
/// Static logger class for MsSql connection factory operations using MessageLogging infrastructure.
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlConnectionFactoryLogger
{
    /// <summary>
    /// Logs when creating a connection.
    /// </summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug, Message = "Creating MsSql connection for '{connectionName}'")]
    public static partial IGenericMessage CreatingConnection(ILogger<MsSqlConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Logs when a connection is created successfully.
    /// </summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug, Message = "Created MsSql connection for '{connectionName}' to server '{server}'")]
    public static partial IGenericMessage ConnectionCreated(ILogger<MsSqlConnectionFactory> logger, string connectionName, string server);

    /// <summary>
    /// Logs when factory receives null configuration.
    /// </summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error, Message = "Factory received null configuration")]
    public static partial IGenericMessage ConfigurationNull(ILogger<MsSqlConnectionFactory> logger);

    /// <summary>
    /// Logs when factory receives invalid configuration type.
    /// </summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Error, Message = "Invalid configuration type. Expected MsSqlConnectionConfiguration, got '{actualType}'")]
    public static partial IGenericMessage InvalidConfigurationType(ILogger<MsSqlConnectionFactory> logger, string actualType);

    /// <summary>
    /// Logs when a connection configuration arrives with no name.
    /// </summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Error, Message = "Connection configuration {connectionId} has no name")]
    public static partial IGenericMessage ConnectionNameMissing(ILogger<MsSqlConnectionFactory> logger, string connectionId);

    /// <summary>
    /// Logs when factory fails to create a connection.
    /// </summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Failed to create MsSql connection for '{connectionName}': {errorMessage}")]
    public static partial IGenericMessage CreationFailed(ILogger<MsSqlConnectionFactory> logger, string connectionName, string errorMessage);

    /// <summary>
    /// Logs when factory receives null context.
    /// </summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Error, Message = "Factory received null creation context for '{connectionName}'")]
    public static partial IGenericMessage ContextNull(ILogger<MsSqlConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Logs when creating a connection with context.
    /// </summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug, Message = "Creating MsSql connection for '{connectionName}' with context")]
    public static partial IGenericMessage CreatingConnectionWithContext(ILogger<MsSqlConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Logs when a connection is created successfully with context.
    /// </summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug, Message = "Created MsSql connection for '{connectionName}' to server '{server}' with context")]
    public static partial IGenericMessage ConnectionCreatedWithContext(ILogger<MsSqlConnectionFactory> logger, string connectionName, string server);

    /// <summary>
    /// Logs when an unsupported connection type is requested.
    /// </summary>
    [MessageLogging(EventId = 21003, Level = LogLevel.Error, Message = "Unsupported connection type: {connectionType}. This factory only supports MsSql.")]
    public static partial IGenericMessage UnsupportedConnectionType(ILogger<MsSqlConnectionFactory> logger, string connectionType);

    /// <summary>
    /// Logs when a created connection is not of the expected type.
    /// </summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Created connection is not of expected type '{expectedType}'")]
    public static partial IGenericMessage UnexpectedConnectionType(ILogger<MsSqlConnectionFactory> logger, string expectedType);

    /// <summary>
    /// Logs when an unsupported authentication type is requested.
    /// </summary>
    [MessageLogging(EventId = 21004, Level = LogLevel.Error, Message = "Unsupported authentication type: '{authType}'. Valid types: SqlAuth, WindowsAuth, EntraId, ManagedIdentity, AzureCli")]
    public static partial IGenericMessage UnsupportedAuthenticationType(ILogger<MsSqlConnectionFactory> logger, string authType);

    /// <summary>
    /// Logs when Authentication.Type is not specified in the configuration.
    /// </summary>
    [MessageLogging(EventId = 21005, Level = LogLevel.Error, Message = "Authentication type not specified for connection '{connectionName}'. Valid types: SqlAuth, WindowsAuth, EntraId, ManagedIdentity, AzureCli")]
    public static partial IGenericMessage AuthenticationTypeNotSpecified(ILogger<MsSqlConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Logs when SecretKeyName is specified but no secret manager is available to resolve it.
    /// </summary>
    [MessageLogging(EventId = 61000, Level = LogLevel.Error, Message = "Connection '{connectionName}' requires secret '{secretKeyName}' but no ISecretManager was provided. Use the async Create overload with a secret manager.")]
    public static partial IGenericMessage SecretManagerRequired(ILogger<MsSqlConnectionFactory> logger, string connectionName, string secretKeyName);

    /// <summary>
    /// Logs when SecretKeyName is specified but no secret manager provider is available.
    /// </summary>
    [MessageLogging(EventId = 61001, Level = LogLevel.Error, Message = "Connection '{connectionName}' requires secret '{secretKeyName}' but SecretManagerProvider is not available")]
    public static partial IGenericMessage SecretManagerProviderNotAvailable(ILogger<MsSqlConnectionFactory> logger, string connectionName, string secretKeyName);

    /// <summary>
    /// Logs when the specified secret manager cannot be found.
    /// </summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Error, Message = "Connection '{connectionName}': Secret manager '{secretManagerName}' not found")]
    public static partial IGenericMessage SecretManagerNotFound(ILogger<MsSqlConnectionFactory> logger, string connectionName, string secretManagerName);

    /// <summary>
    /// Logs when the specified secret cannot be found.
    /// </summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error, Message = "Connection '{connectionName}': Secret '{secretKeyName}' not found")]
    public static partial IGenericMessage SecretNotFound(ILogger<MsSqlConnectionFactory> logger, string connectionName, string secretKeyName);

    /// <summary>
    /// Logs when authentication processing fails.
    /// </summary>
    [MessageLogging(EventId = 51000, Level = LogLevel.Error, Message = "Failed to process authentication")]
    public static partial IGenericMessage AuthenticationProcessingFailed(ILogger<MsSqlConnectionFactory> logger);

    /// <summary>
    /// Logs when acquiring an access token for token-based authentication.
    /// </summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace, Message = "Acquiring access token for connection '{connectionName}'")]
    public static partial IGenericMessage AcquiringAccessToken(ILogger<MsSqlConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Logs when an access token was acquired successfully.
    /// </summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace, Message = "Access token acquired for connection '{connectionName}'")]
    public static partial IGenericMessage AccessTokenAcquired(ILogger<MsSqlConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Logs when access token acquisition fails.
    /// </summary>
    [MessageLogging(EventId = 51001, Level = LogLevel.Error, Message = "Failed to acquire access token for connection '{connectionName}': {errorMessage}")]
    public static partial IGenericMessage AccessTokenAcquisitionFailed(ILogger<MsSqlConnectionFactory> logger, string connectionName, string errorMessage);

    // ═══════════════════════════════════════════════════════════════════════════
    // Trace-Level Diagnostic Events (3124-3130)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traces entry into the Create method with generic configuration.
    /// </summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Trace, Message = "Entering MsSqlConnectionFactory.Create with IGenericConfiguration '{configurationType}'")]
    public static partial IGenericMessage TraceCreateGenericEntry(ILogger<MsSqlConnectionFactory> logger, string configurationType);

    /// <summary>
    /// Traces entry into the Create method with typed configuration.
    /// </summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Trace, Message = "Entering MsSqlConnectionFactory.Create for connection '{connectionName}'")]
    public static partial IGenericMessage TraceCreateEntry(ILogger<MsSqlConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Traces entry into the async Create method with context.
    /// </summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace, Message = "Entering MsSqlConnectionFactory.Create (async) for connection '{connectionName}'")]
    public static partial IGenericMessage TraceCreateAsyncEntry(ILogger<MsSqlConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Traces connection string building.
    /// </summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace, Message = "Building connection string for '{connectionName}' with auth type '{authType}'")]
    public static partial IGenericMessage TraceBuildingConnectionString(ILogger<MsSqlConnectionFactory> logger, string connectionName, string authType);

    /// <summary>
    /// Traces authentication type resolution.
    /// </summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace, Message = "Resolved authentication type '{authType}' for connection '{connectionName}'")]
    public static partial IGenericMessage TraceAuthTypeResolved(ILogger<MsSqlConnectionFactory> logger, string authType, string connectionName);

    /// <summary>
    /// Traces secret resolution start.
    /// </summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Trace, Message = "Resolving secret '{secretKeyName}' for connection '{connectionName}'")]
    public static partial IGenericMessage TraceResolvingSecret(ILogger<MsSqlConnectionFactory> logger, string secretKeyName, string connectionName);

    /// <summary>
    /// Traces Create with connection type specification.
    /// </summary>
    [MessageLogging(EventId = 11012, Level = LogLevel.Trace, Message = "Entering MsSqlConnectionFactory.Create with connectionType '{connectionType}' for '{configurationType}'")]
    public static partial IGenericMessage TraceCreateWithTypeEntry(ILogger<MsSqlConnectionFactory> logger, string connectionType, string configurationType);

    /// <summary>
    /// Traces the resolved connection configuration before building the connection string.
    /// </summary>
    [MessageLogging(EventId = 11013, Level = LogLevel.Trace, Message = "MsSql connection '{connectionName}' config: Server='{server}', Database='{database}', Port={port}, AuthType='{authType}', Encrypt={encrypt}, TrustServerCertificate={trustCert}")]
    public static partial IGenericMessage TraceConnectionConfig(ILogger<MsSqlConnectionFactory> logger, string connectionName, string server, string database, int port, string authType, bool encrypt, bool trustCert);

    /// <summary>
    /// Logs that the connection's authentication type requires a secret but the factory was built
    /// without a secret-manager provider and none was supplied to Create.
    /// </summary>
    [MessageLogging(EventId = 21006, Level = LogLevel.Error, Message = "Connection '{connectionName}' uses authentication type '{authType}', which requires a secret, but this factory has no secret-manager provider and none was supplied — build it through the connection provider or pass a secret manager")]
    public static partial IGenericMessage SecretManagerRequiredButNotProvided(ILogger<MsSqlConnectionFactory> logger, string connectionName, string authType);

    /// <summary>
    /// Logs that the supplied secret manager is not the one the connection declares.
    /// </summary>
    [MessageLogging(EventId = 21007, Level = LogLevel.Error, Message = "Connection '{connectionName}' declares secret manager '{declared}' but '{supplied}' was supplied — refusing to read a secret from a store this connection did not name")]
    public static partial IGenericMessage SecretManagerMismatch(ILogger<MsSqlConnectionFactory> logger, string connectionName, string declared, string supplied);

    /// <summary>
    /// Logs that the connection's AuthenticationType matched no registered authentication type.
    /// </summary>
    [MessageLogging(EventId = 21009, Level = LogLevel.Error, Message = "Connection '{connectionName}' has AuthenticationType '{authType}', which matches no registered MsSql authentication type")]
    public static partial IGenericMessage AuthenticationTypeUnknown(ILogger<MsSqlConnectionFactory> logger, string connectionName, string authType);

    /// <summary>
    /// Traces that no secret manager was involved because the authentication type does not need one.
    /// </summary>
    // Why: the quiet, expected case — WindowsAuth, EntraId, ManagedIdentity and AzureCli declare no
    // secret properties, so the factory never asks for a secret manager. Logged at Debug so a support
    // question about "why was the secret store never called" is answerable from the log.
    [MessageLogging(EventId = 11014, Level = LogLevel.Debug, Message = "Connection '{connectionName}' built without a secret manager — authentication type '{authType}' does not require a secret")]
    public static partial IGenericMessage BuiltWithoutSecretManager(ILogger<MsSqlConnectionFactory> logger, string connectionName, string authType);

    /// <summary>
    /// Traces that the declared secret manager was resolved and the secret read.
    /// </summary>
    [MessageLogging(EventId = 11015, Level = LogLevel.Debug, Message = "Connection '{connectionName}' resolved its secret from secret manager '{declared}'")]
    public static partial IGenericMessage SecretResolvedFromManager(ILogger<MsSqlConnectionFactory> logger, string connectionName, string declared);
}
