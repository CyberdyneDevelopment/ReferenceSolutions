using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;
using ReferenceSecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.AzureKeyVault.Logging;

/// <summary>
/// Message logging for Azure Key Vault secret manager factory operations.
/// Follows the MessageLogging pattern: log AND return the message.
/// </summary>
[MessageLoggingTypeCode("AZUREKEYVAULT")]
public static partial class AzureKeyVaultSecretManagerFactoryLogger
{
    /// <summary>
    /// Logs when creating a secret manager.
    /// </summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Debug, Message = "Creating Azure Key Vault secret manager '{secretManagerName}'")]
    public static partial IGenericMessage CreatingSecretManager(ILogger<AzureKeyVaultSecretManagerFactory> logger, string secretManagerName);

    /// <summary>
    /// Logs when a secret manager is created successfully.
    /// </summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Debug, Message = "Created Azure Key Vault secret manager '{secretManagerName}' for vault '{vaultUri}'")]
    public static partial IGenericMessage SecretManagerCreated(ILogger<AzureKeyVaultSecretManagerFactory> logger, string secretManagerName, string vaultUri);

    /// <summary>
    /// Logs when factory receives null configuration.
    /// </summary>
    [MessageLogging(EventId = 21006, Level = LogLevel.Error, Message = "Factory received null configuration")]
    public static partial IGenericMessage ConfigurationNull(ILogger<AzureKeyVaultSecretManagerFactory> logger);

    /// <summary>
    /// Logs when factory receives invalid configuration type.
    /// </summary>
    [MessageLogging(EventId = 21007, Level = LogLevel.Error, Message = "Invalid configuration type. Expected AzureKeyVaultConfiguration, got '{actualType}'")]
    public static partial IGenericMessage InvalidConfigurationType(ILogger<AzureKeyVaultSecretManagerFactory> logger, string actualType);

    /// <summary>
    /// Logs when factory fails to create a secret manager.
    /// </summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error, Message = "Failed to create Azure Key Vault secret manager '{secretManagerName}': {errorMessage}")]
    public static partial IGenericMessage CreationFailed(ILogger<AzureKeyVaultSecretManagerFactory> logger, string secretManagerName, string errorMessage);

    /// <summary>
    /// Logs when factory receives null context.
    /// </summary>
    [MessageLogging(EventId = 21008, Level = LogLevel.Error, Message = "Factory received null creation context for '{secretManagerName}'")]
    public static partial IGenericMessage ContextNull(ILogger<AzureKeyVaultSecretManagerFactory> logger, string secretManagerName);

    /// <summary>
    /// Logs when creating a secret manager with context.
    /// </summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Debug, Message = "Creating Azure Key Vault secret manager '{secretManagerName}' with context")]
    public static partial IGenericMessage CreatingSecretManagerWithContext(ILogger<AzureKeyVaultSecretManagerFactory> logger, string secretManagerName);

    /// <summary>
    /// Logs when a secret manager is created successfully with context.
    /// </summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Debug, Message = "Created Azure Key Vault secret manager '{secretManagerName}' for vault '{vaultUri}' with context")]
    public static partial IGenericMessage SecretManagerCreatedWithContext(ILogger<AzureKeyVaultSecretManagerFactory> logger, string secretManagerName, string vaultUri);

    /// <summary>
    /// Logs when a created secret manager is not of the expected type.
    /// </summary>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error, Message = "Created secret manager is not of expected type '{expectedType}'")]
    public static partial IGenericMessage UnexpectedSecretManagerType(ILogger<AzureKeyVaultSecretManagerFactory> logger, string expectedType);

    /// <summary>
    /// Logs when vault URI is missing from configuration.
    /// </summary>
    [MessageLogging(EventId = 21009, Level = LogLevel.Error, Message = "Vault URI is required for secret manager '{secretManagerName}'")]
    public static partial IGenericMessage VaultUriRequired(ILogger<AzureKeyVaultSecretManagerFactory> logger, string secretManagerName);
}
