using System;
using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSecretManagers.HashiCorpVault.Logging;

/// <summary>
/// MessageLogging for the HashiCorp Vault secret manager.
/// </summary>
/// <remarks>
/// <para>
/// EventIds are categorized numbers (<c>Category = Id / 10000</c>) drawn from this package's open
/// band: 11xxx non-error, 31xxx not-found, 51xxx auth, 61xxx configuration, 71xxx dependency/IO,
/// 91xxx internal.
/// </para>
/// <para>
/// <b>No method here takes a secret, a Vault token, or an issued password.</b> What an operator needs
/// to diagnose a Vault problem is the address, the engine, the path, the auth method and Vault's own
/// error text — none of which is a credential.
/// </para>
/// </remarks>
[MessageLoggingTypeCode("VAULT")]
public static partial class VaultLog
{
    /// <summary>Logs that the Vault secret manager registered itself.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="optionName">The registered option name.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace, Message = "Secret manager registered: {optionName}")]
    public static partial IGenericMessage SecretManagerRegistered(ILogger logger, string optionName);

    /// <summary>Logs that a Vault login is being attempted.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="authMethod">The auth method being used.</param>
    /// <param name="mount">The auth mount path.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug, Message = "Logging in to Vault: configuration '{configurationName}' via {authMethod} at mount '{mount}'")]
    public static partial IGenericMessage LoggingIn(ILogger logger, string configurationName, string authMethod, string mount);

    /// <summary>Logs a successful Vault login.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="authMethod">The auth method used.</param>
    /// <param name="expiresAt">When the issued Vault token's lease ends.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information, Message = "Vault login succeeded: configuration '{configurationName}' via {authMethod}, token lease ends {expiresAt:O}")]
    public static partial IGenericMessage LoggedIn(ILogger logger, string configurationName, string authMethod, DateTimeOffset expiresAt);

    /// <summary>Logs that a secret read is starting.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="engine">The secret engine being read through.</param>
    /// <param name="path">The Vault path being read.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace, Message = "Reading from Vault: configuration '{configurationName}' via {engine} at '{path}'")]
    public static partial IGenericMessage ReadingSecret(ILogger logger, string configurationName, string engine, string path);

    /// <summary>Logs that a stored secret was read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="engine">The secret engine used.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug, Message = "Vault secret read: configuration '{configurationName}' via {engine}")]
    public static partial IGenericMessage SecretRead(ILogger logger, string configurationName, string engine);

    /// <summary>Logs that Vault issued a fresh, lease-bound credential.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="role">The Vault role the credential was issued against.</param>
    /// <param name="expiresAt">When the credential's lease ends, or a note that it carried none.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information, Message = "Vault issued a dynamic credential: configuration '{configurationName}' role '{role}', lease ends {expiresAt}")]
    public static partial IGenericMessage CredentialIssued(ILogger logger, string configurationName, string role, string expiresAt);

    /// <summary>Logs the Vault request as it is about to be sent.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The Vault path below /v1/.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11006, Level = LogLevel.Trace, Message = "Vault request: configuration '{configurationName}' {method} /v1/{path}")]
    public static partial IGenericMessage SendingRequest(ILogger logger, string configurationName, string method, string path);

    /// <summary>Logs the raw status Vault answered with.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="path">The Vault path that answered.</param>
    /// <param name="statusCode">The HTTP status returned.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11007, Level = LogLevel.Trace, Message = "Vault answered: configuration '{configurationName}' /v1/{path} status={statusCode}")]
    public static partial IGenericMessage RequestAnswered(ILogger logger, string configurationName, string path, int statusCode);

    /// <summary>Logs that the held Vault token is still live, so no login is needed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="expiresAt">When the held token's lease ends.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace, Message = "Reusing held Vault token: configuration '{configurationName}', lease ends {expiresAt:O}")]
    public static partial IGenericMessage TokenReused(ILogger logger, string configurationName, DateTimeOffset expiresAt);

    /// <summary>Logs that the held Vault token is absent or too close to expiry, so a login will run.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="reason">Why a login is needed.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace, Message = "Vault login required: configuration '{configurationName}' — {reason}")]
    public static partial IGenericMessage LoginRequired(ILogger logger, string configurationName, string reason);

    /// <summary>Logs that the login credential was resolved and the login is about to be posted.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="authMethod">The auth method.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace, Message = "Vault login credential resolved: configuration '{configurationName}' via {authMethod}")]
    public static partial IGenericMessage LoginCredentialResolved(ILogger logger, string configurationName, string authMethod);

    /// <summary>Logs that Vault cannot serve this configuration at all.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 91002, Level = LogLevel.Critical, Message = "The Vault secret manager cannot serve '{configurationName}' — every secret it backs is unavailable")]
    public static partial IGenericMessage SecretManagerUnusable(ILogger logger, string configurationName);

    /// <summary>Logs that the requested Vault path holds nothing.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="path">The Vault path that was read.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 31000, Level = LogLevel.Error, Message = "Vault has nothing at '{path}' for configuration '{configurationName}'")]
    public static partial IGenericMessage SecretNotFound(ILogger logger, string configurationName, string path);

    /// <summary>Logs that Vault rejected this process's login.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="authMethod">The auth method that was rejected.</param>
    /// <param name="error">Vault's own error text.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 51000, Level = LogLevel.Error, Message = "Vault rejected this process's credential: configuration '{configurationName}' via {authMethod} — {error}")]
    public static partial IGenericMessage AuthenticationRejected(ILogger logger, string configurationName, string authMethod, string error);

    /// <summary>Logs that Vault authenticated this process but its policy forbids the path.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="path">The Vault path that was refused.</param>
    /// <param name="error">Vault's own error text.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 51001, Level = LogLevel.Error, Message = "Vault policy does not permit '{path}' for configuration '{configurationName}' — {error}")]
    public static partial IGenericMessage PermissionDenied(ILogger logger, string configurationName, string path, string error);

    /// <summary>Logs that a Vault configuration is missing a value it cannot run without.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="property">The property that has no value.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 61000, Level = LogLevel.Critical, Message = "Vault configuration '{configurationName}' is missing required value '{property}'")]
    public static partial IGenericMessage ConfigurationValueMissing(ILogger logger, string configurationName, string property);

    /// <summary>Logs that a Vault configuration names an auth method or engine that is not registered.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="kind">What was being selected (auth method, secret engine).</param>
    /// <param name="requested">The name the configuration asked for.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 61001, Level = LogLevel.Critical, Message = "Vault configuration '{configurationName}' names {kind} '{requested}', which is not registered")]
    public static partial IGenericMessage OptionNotRegistered(ILogger logger, string configurationName, string kind, string requested);

    /// <summary>Logs that a command this secret manager does not implement was submitted.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="operation">The operation that was asked for.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 61002, Level = LogLevel.Error, Message = "The Vault secret manager does not implement '{operation}' (configuration '{configurationName}')")]
    public static partial IGenericMessage OperationNotSupported(ILogger logger, string configurationName, string operation);

    /// <summary>Logs that Vault could not be reached.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="ex">The transport exception.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="address">The Vault address that could not be reached.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Could not reach Vault at '{address}' for configuration '{configurationName}'")]
    public static partial IGenericMessage VaultUnreachable(ILogger logger, Exception ex, string configurationName, string address);

    /// <summary>Logs that Vault returned an unexpected status.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="address">The Vault address.</param>
    /// <param name="statusCode">The HTTP status returned.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error, Message = "Vault at '{address}' returned {statusCode} for configuration '{configurationName}'")]
    public static partial IGenericMessage VaultReturnedError(ILogger logger, string configurationName, string address, int statusCode);

    /// <summary>Logs that a Vault error response was not the JSON its API contract calls for.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="ex">The parse exception.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 71002, Level = LogLevel.Warning, Message = "Vault error response for configuration '{configurationName}' was not JSON — the request may not have reached Vault's API")]
    public static partial IGenericMessage ErrorResponseUnparseable(ILogger logger, Exception ex, string configurationName);

    /// <summary>Logs that a Vault response could not be read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="ex">The parse exception.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="address">The Vault address.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Could not read Vault's response for configuration '{configurationName}' at '{address}'")]
    public static partial IGenericMessage ResponseUnreadable(ILogger logger, Exception ex, string configurationName, string address);

    /// <summary>Logs that a Vault response was well-formed but incomplete.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The secret manager configuration.</param>
    /// <param name="context">The engine or auth method whose response it was.</param>
    /// <param name="field">The field the response did not carry.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error, Message = "Vault response for configuration '{configurationName}' ({context}) carried no '{field}'")]
    public static partial IGenericMessage ResponseIncomplete(ILogger logger, string configurationName, string context, string field);
}
