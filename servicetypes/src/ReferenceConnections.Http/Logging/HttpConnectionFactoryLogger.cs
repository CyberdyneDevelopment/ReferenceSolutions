using Fdw.Configuration;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace ReferenceConnections.Http.Logging;

/// <summary>
/// Static logger class for HTTP connection factory operations using MessageLogging infrastructure.
/// </summary>
[MessageLoggingTypeCode("HTTP")]
public static partial class HttpConnectionFactoryLogger
{
    /// <summary>
    /// Logs when an mTLS HTTP client is created with a client certificate.
    /// </summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug, Message = "Created mTLS HTTP client with client certificate for connection '{connectionName}'")]
    public static partial IGenericMessage MtlsClientCreated(ILogger<HttpConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Logs when mTLS is configured but no client certificate was resolved.
    /// </summary>
    // Why Error, not Warning (FDW-583): a security downgrade (mTLS was required but the client falls
    // back to standard HTTP) — level changed per this audit; the fallback behavior itself is
    // intentionally left untouched (a separate decision, HOLD per FDW-583 brief).
    [MessageLogging(EventId = 61000, Level = LogLevel.Error, Message = "UseMtls is enabled for connection '{connectionName}' but no client certificate was resolved — falling back to standard HTTP client")]
    public static partial IGenericMessage MtlsCertificateRequiredButMissing(ILogger<HttpConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Logs when creating a connection.
    /// </summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug, Message = "Creating HTTP connection for '{connectionName}'")]
    public static partial IGenericMessage CreatingConnection(ILogger<HttpConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Logs when a connection is created successfully.
    /// </summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug, Message = "Created HTTP connection for '{connectionName}' to '{baseUrl}'")]
    public static partial IGenericMessage ConnectionCreated(ILogger<HttpConnectionFactory> logger, string connectionName, string baseUrl);

    /// <summary>
    /// Logs when factory receives null configuration.
    /// </summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error, Message = "Factory received null configuration")]
    public static partial IGenericMessage ConfigurationNull(ILogger<HttpConnectionFactory> logger);

    /// <summary>
    /// Logs when factory receives invalid configuration type.
    /// </summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Error, Message = "Invalid configuration type. Expected HttpConnectionConfiguration, got '{actualType}'")]
    public static partial IGenericMessage InvalidConfigurationType(ILogger<HttpConnectionFactory> logger, string actualType);

    /// <summary>
    /// Logs when factory fails to create a connection.
    /// </summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Failed to create HTTP connection for '{connectionName}': {errorMessage}")]
    public static partial IGenericMessage CreationFailed(ILogger<HttpConnectionFactory> logger, string connectionName, string errorMessage);

    /// <summary>
    /// Logs when factory receives null context.
    /// </summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Error, Message = "Factory received null creation context for '{connectionName}'")]
    public static partial IGenericMessage ContextNull(ILogger<HttpConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Logs when creating a connection with context.
    /// </summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug, Message = "Creating HTTP connection for '{connectionName}' with context")]
    public static partial IGenericMessage CreatingConnectionWithContext(ILogger<HttpConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Logs when a connection is created successfully with context.
    /// </summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Debug, Message = "Created HTTP connection for '{connectionName}' to '{baseUrl}' with context")]
    public static partial IGenericMessage ConnectionCreatedWithContext(ILogger<HttpConnectionFactory> logger, string connectionName, string baseUrl);

    /// <summary>
    /// Logs when an unsupported connection type is requested.
    /// </summary>
    [MessageLogging(EventId = 21003, Level = LogLevel.Error, Message = "Unsupported connection type: {connectionType}. This factory only supports Http.")]
    public static partial IGenericMessage UnsupportedConnectionType(ILogger<HttpConnectionFactory> logger, string connectionType);

    /// <summary>
    /// Logs when a created connection is not of the expected type.
    /// </summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error, Message = "Created connection is not of expected type '{expectedType}'")]
    public static partial IGenericMessage UnexpectedConnectionType(ILogger<HttpConnectionFactory> logger, string expectedType);

    /// <summary>
    /// Logs when registering a factory or config provider with the connection provider fails.
    /// </summary>
    [MessageLogging(EventId = 61001, Level = LogLevel.Critical, Message = "Failed to register '{registrationName}' with connection provider: {error}")]
    public static partial IGenericMessage RegistrationFailed(ILogger logger, string registrationName, string error);

    /// <summary>
    /// Logs when configuring HTTP client.
    /// </summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Debug, Message = "Configuring HTTP client for '{connectionName}' with base URL: {baseUrl}, timeout: {timeout}s")]
    public static partial IGenericMessage ConfiguringHttpClient(ILogger<HttpConnectionFactory> logger, string connectionName, string baseUrl, int timeout);

    /// <summary>
    /// Logs when no translator is provided.
    /// </summary>
    [MessageLogging(EventId = 61002, Level = LogLevel.Error, Message = "No data command translator configured for HTTP connection '{connectionName}'")]
    public static partial IGenericMessage NoTranslatorConfigured(ILogger<HttpConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Logs when protocol is not specified.
    /// </summary>
    [MessageLogging(EventId = 61003, Level = LogLevel.Warning, Message = "No protocol specified for HTTP connection '{connectionName}', defaulting to REST")]
    public static partial IGenericMessage NoProtocolSpecified(ILogger<HttpConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Logs when protocol is not found in TypeCollection.
    /// </summary>
    [MessageLogging(EventId = 61004, Level = LogLevel.Error, Message = "Protocol '{protocolName}' not found for HTTP connection '{connectionName}'")]
    public static partial IGenericMessage ProtocolNotFound(ILogger<HttpConnectionFactory> logger, string connectionName, string protocolName);

    /// <summary>
    /// Logs when secret manager is not configured.
    /// </summary>
    [MessageLogging(EventId = 61005, Level = LogLevel.Error, Message = "Secret manager not configured for HTTP connection '{connectionName}'")]
    public static partial IGenericMessage SecretManagerNotConfigured(ILogger<HttpConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Logs when secret manager is not found.
    /// </summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Error, Message = "Secret manager '{managerName}' not found for HTTP connection '{connectionName}'")]
    public static partial IGenericMessage SecretManagerNotFound(ILogger<HttpConnectionFactory> logger, string connectionName, string managerName);

    /// <summary>
    /// Logs when secret is not found.
    /// </summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error, Message = "Secret '{secretName}' not found for HTTP connection '{connectionName}'")]
    public static partial IGenericMessage SecretNotFound(ILogger<HttpConnectionFactory> logger, string connectionName, string secretName);

    /// <summary>
    /// Logs when certificate fails to load.
    /// </summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Failed to load certificate '{secretName}' for HTTP connection '{connectionName}': {errorMessage}")]
    public static partial IGenericMessage CertificateLoadFailed(ILogger<HttpConnectionFactory> logger, string connectionName, string secretName, string errorMessage);

    // ═══════════════════════════════════════════════════════════════════════════
    // Trace-Level Diagnostic Events (7119-7124)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traces entry into the Create method with generic configuration.
    /// </summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Trace, Message = "Entering HttpConnectionFactory.Create with IGenericConfiguration '{configurationType}'")]
    public static partial IGenericMessage TraceCreateGenericEntry(ILogger<HttpConnectionFactory> logger, string configurationType);

    /// <summary>
    /// Traces entry into the Create method with typed configuration.
    /// </summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Trace, Message = "Entering HttpConnectionFactory.Create for connection '{connectionName}'")]
    public static partial IGenericMessage TraceCreateEntry(ILogger<HttpConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Traces protocol resolution.
    /// </summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace, Message = "Resolving protocol '{protocolName}' for HTTP connection '{connectionName}'")]
    public static partial IGenericMessage TraceResolvingProtocol(ILogger<HttpConnectionFactory> logger, string protocolName, string connectionName);

    /// <summary>
    /// Traces HttpClient creation.
    /// </summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace, Message = "Creating HttpClient for connection '{connectionName}' with base URL '{baseUrl}'")]
    public static partial IGenericMessage TraceCreatingHttpClient(ILogger<HttpConnectionFactory> logger, string connectionName, string baseUrl);

    /// <summary>
    /// Traces entry into the Create method with explicit dependencies.
    /// </summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace, Message = "Entering HttpConnectionFactory.Create with explicit dependencies for connection '{connectionName}'")]
    public static partial IGenericMessage TraceCreateWithDepsEntry(ILogger<HttpConnectionFactory> logger, string connectionName);

    /// <summary>
    /// Traces entry into the Create method with connection type specification.
    /// </summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Trace, Message = "Entering HttpConnectionFactory.Create with connectionType '{connectionType}' for '{configurationType}'")]
    public static partial IGenericMessage TraceCreateWithTypeEntry(ILogger<HttpConnectionFactory> logger, string connectionType, string configurationType);
}
