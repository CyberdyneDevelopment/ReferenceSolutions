using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;
using Fdw.Services.SecretManagers.UserSecrets.Commands;
using Fdw.Services.SecretManagers.UserSecrets.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.UserSecrets.Logging;

/// <summary>
/// Message logging for User Secrets secret manager factory operations.
/// </summary>
/// <remarks>
/// <para>
/// This class follows the MessageLogging pattern where all operations are logged AND
/// the log message is returned in the result. Every failure mode has an explicit
/// MessageLogging method defined here.
/// </para>
/// <para>
/// Event IDs are in the 5200-5299 range for factory operations.
/// </para>
/// </remarks>
[MessageLoggingTypeCode("USERSECRETS")]
public static partial class UserSecretsFactoryLogger
{
    /// <summary>
    /// Logs when creating a User Secrets secret manager.
    /// </summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug, Message = "Creating User Secrets secret manager for '{name}'")]
    public static partial IGenericMessage CreatingSecretManager(ILogger logger, string name);

    /// <summary>
    /// Logs when a User Secrets secret manager is created successfully.
    /// </summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug, Message = "Created User Secrets secret manager for '{name}' with UserSecretsId '{userSecretsId}'")]
    public static partial IGenericMessage SecretManagerCreated(ILogger logger, string name, string userSecretsId);

    /// <summary>
    /// Logs when factory receives null configuration.
    /// </summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error, Message = "Factory received null configuration")]
    public static partial IGenericMessage ConfigurationNull(ILogger logger);

    /// <summary>
    /// Logs when factory receives invalid configuration type.
    /// </summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Error, Message = "Invalid configuration type. Expected {expectedType}, got '{actualType}'")]
    public static partial IGenericMessage InvalidConfigurationType(ILogger logger, string expectedType, string actualType);

    /// <summary>
    /// Logs when factory fails to create a secret manager.
    /// </summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Failed to create User Secrets secret manager for '{name}': {errorMessage}")]
    public static partial IGenericMessage CreationFailed(ILogger logger, string name, string errorMessage);

    /// <summary>
    /// Logs when a created service is not of the expected type.
    /// </summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error, Message = "Created service is not of expected type '{expectedType}'")]
    public static partial IGenericMessage UnexpectedServiceType(ILogger logger, string expectedType);
}
