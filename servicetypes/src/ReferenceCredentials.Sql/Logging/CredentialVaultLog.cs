using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;
using Fdw.Services.Credentials;
using Fdw.Services.Credentials.Logging;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services.Credentials.Sql.Options;
using Fdw.Services.Credentials.Sql.Outcomes;
using Fdw.Services.DataVault;
using Fdw.Services.DataVault.Logging;
using Fdw.Services;
using Fdw;

namespace ReferenceCredentials.Sql.Logging;

/// <summary>
/// MessageLogging for the credential (password) vault verbs. The operation and outcome are logged —
/// never hash, salt, or pepper material.
/// EventId range: 7966-7989.
/// </summary>
[MessageLoggingTypeCode("SQL")]
public static partial class CredentialVaultLog
{
    /// <summary>
    /// Logs that credential validation has started for a user.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="userId">The identifier of the user whose credential is being validated.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "Validating credential for user '{userId}'")]
    public static partial IGenericMessage ValidateStarted(ILogger logger, System.Guid userId);

    /// <summary>
    /// Logs that credential validation completed for a user with a given outcome.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="userId">The identifier of the user whose credential was validated.</param>
    /// <param name="outcome">The outcome of the credential validation.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug,
        Message = "Credential validation completed for user '{userId}' with outcome '{outcome}'")]
    public static partial IGenericMessage ValidateCompleted(ILogger logger, System.Guid userId, string outcome);

    /// <summary>
    /// Logs that no current secret is on file for a user, and the negative path ran the same constant-time work.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="userId">The identifier of the user that has no current secret on file.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11004, Level = LogLevel.Debug,
        Message = "No current secret on file for user '{userId}' — negative path ran the same constant-time work")]
    public static partial IGenericMessage NoSecretOnFile(ILogger logger, System.Guid userId);

    /// <summary>
    /// Logs that a new current secret was stored for a user.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="userId">The identifier of the user a new current secret was stored for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information,
        Message = "Stored a new current secret for user '{userId}'")]
    public static partial IGenericMessage SecretCreated(ILogger logger, System.Guid userId);

    /// <summary>
    /// Logs that the current secret was changed for a user.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="userId">The identifier of the user whose current secret was changed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information,
        Message = "Changed the current secret for user '{userId}'")]
    public static partial IGenericMessage SecretChanged(ILogger logger, System.Guid userId);

    /// <summary>
    /// Logs that a secret change was rejected because the supplied current secret did not match.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="userId">The identifier of the user whose secret change was rejected.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning,
        Message = "Change rejected for user '{userId}' — the supplied current secret did not match")]
    public static partial IGenericMessage ChangeRejectedOldMismatch(ILogger logger, System.Guid userId);

    /// <summary>
    /// Logs that the current secret was disabled (soft-retired) for a user.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="userId">The identifier of the user whose current secret was disabled.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11007, Level = LogLevel.Information,
        Message = "Disabled (soft-retired) the current secret for user '{userId}'")]
    public static partial IGenericMessage SecretDisabled(ILogger logger, System.Guid userId);

    /// <summary>
    /// Logs that there is no current secret to disable for a user, so nothing was retired.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="userId">The identifier of the user that has no current secret to disable.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning,
        Message = "No current secret to disable for user '{userId}' — nothing to retire")]
    public static partial IGenericMessage NoCurrentSecretToDisable(ILogger logger, System.Guid userId);

    /// <summary>
    /// Logs that a credential write affected no rows for a user during a given operation.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="userId">The identifier of the user whose credential write affected no rows.</param>
    /// <param name="operation">The credential operation during which the write affected no rows.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "Credential write affected no rows for user '{userId}' during '{operation}'")]
    public static partial IGenericMessage WriteAffectedNoRows(ILogger logger, System.Guid userId, string operation);

    /// <summary>
    /// Logs that a credential operation received an empty derived hash and is refusing to proceed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="operation">The credential operation that received an empty derived hash.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error,
        Message = "Credential operation '{operation}' received an empty derived hash — refusing to proceed")]
    public static partial IGenericMessage DerivedHashMissing(ILogger logger, string operation);
}
