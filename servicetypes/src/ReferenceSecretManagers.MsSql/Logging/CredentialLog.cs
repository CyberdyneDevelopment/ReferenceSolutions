using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;
using Fdw.Services.SecretManagers.MsSql.Commands;
using Fdw.Services.SecretManagers.MsSql.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.MsSql.Logging;

/// <summary>
/// Message logging for MsSql credential operations.
/// EventId range: 6065-6079
/// </summary>
[MessageLoggingTypeCode("MSSQL2")]
public static partial class CredentialLog
{
    /// <summary>
    /// Logs that a credential verification attempt is being made for a user with a given credential type.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The identifier of the user whose credential is being verified.</param>
    /// <param name="credentialType">The type of credential being verified.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug, Message = "Credential verification attempt for user '{userId}' with type '{credentialType}'")]
    public static partial IGenericMessage VerificationAttempt(ILogger logger, Guid userId, string credentialType);

    /// <summary>
    /// Logs that credential verification succeeded for a user with a given credential type.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The identifier of the user whose credential was verified.</param>
    /// <param name="credentialType">The type of credential that was verified.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information, Message = "Credential verification succeeded for user '{userId}' with type '{credentialType}'")]
    public static partial IGenericMessage VerificationSuccess(ILogger logger, Guid userId, string credentialType);

    /// <summary>
    /// Logs that credential verification failed for a user with a given credential type.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The identifier of the user whose credential verification failed.</param>
    /// <param name="credentialType">The type of credential that failed verification.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 51000, Level = LogLevel.Warning, Message = "Credential verification failed for user '{userId}' with type '{credentialType}'")]
    public static partial IGenericMessage VerificationFailed(ILogger logger, Guid userId, string credentialType);

    /// <summary>
    /// Logs that a credential was stored for a user with a given credential type.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The identifier of the user the credential was stored for.</param>
    /// <param name="credentialType">The type of credential that was stored.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information, Message = "Credential stored for user '{userId}' with type '{credentialType}'")]
    public static partial IGenericMessage CredentialStored(ILogger logger, Guid userId, string credentialType);

    /// <summary>
    /// Logs that a credential was revoked for a given credential type.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="credentialId">The identifier of the credential that was revoked.</param>
    /// <param name="credentialType">The type of credential that was revoked.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information, Message = "Credential '{credentialId}' revoked for type '{credentialType}'")]
    public static partial IGenericMessage CredentialRevoked(ILogger logger, Guid credentialId, string credentialType);

    /// <summary>
    /// Logs that the password hasher is not available in the execution context.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61000, Level = LogLevel.Error, Message = "Password hasher is not available in execution context")]
    public static partial IGenericMessage PasswordHasherNotAvailable(ILogger logger);

    /// <summary>
    /// Logs that the token hasher is not available in the execution context.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61001, Level = LogLevel.Error, Message = "Token hasher is not available in execution context")]
    public static partial IGenericMessage TokenHasherNotAvailable(ILogger logger);

    /// <summary>
    /// Logs that the token generator is not available in the execution context.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61002, Level = LogLevel.Error, Message = "Token generator is not available in execution context")]
    public static partial IGenericMessage TokenGeneratorNotAvailable(ILogger logger);

    /// <summary>
    /// Logs that an unsupported credential type was encountered.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="credentialType">The unsupported credential type.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error, Message = "Unsupported credential type '{credentialType}'")]
    public static partial IGenericMessage UnsupportedCredentialType(ILogger logger, string credentialType);

    /// <summary>
    /// Logs that no password hash was found for a user.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The identifier of the user with no password hash.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning, Message = "No password hash found for user '{userId}'")]
    public static partial IGenericMessage NoPasswordHashFound(ILogger logger, Guid userId);

    /// <summary>
    /// Logs that an API key was not found or had already been revoked.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 51001, Level = LogLevel.Warning, Message = "API key not found or already revoked")]
    public static partial IGenericMessage ApiKeyNotFound(ILogger logger);

    /// <summary>
    /// Logs that an API key has expired for a user.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The identifier of the user whose API key has expired.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 51002, Level = LogLevel.Warning, Message = "API key has expired for user '{userId}'")]
    public static partial IGenericMessage ApiKeyExpired(ILogger logger, Guid userId);

    /// <summary>
    /// Logs that a credential operation failed with the given error.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception that caused the operation to fail.</param>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Credential operation failed: {errorMessage}")]
    public static partial IGenericMessage OperationFailed(ILogger logger, Exception exception, string errorMessage);

    /// <summary>
    /// Logs that the HMAC key is not configured for API key hashing.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61003, Level = LogLevel.Error, Message = "HMAC key is not configured for API key hashing")]
    public static partial IGenericMessage HmacKeyNotConfigured(ILogger logger);

    /// <summary>
    /// Logs the execution of a credential SQL query for a given operation and schema.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="operation">The name of the credential operation being executed.</param>
    /// <param name="schema">The schema the query is executed against.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace, Message = "Executing credential SQL query for '{operation}' on [{schema}]")]
    public static partial IGenericMessage TraceCredentialQuery(ILogger logger, string operation, string schema);
}
