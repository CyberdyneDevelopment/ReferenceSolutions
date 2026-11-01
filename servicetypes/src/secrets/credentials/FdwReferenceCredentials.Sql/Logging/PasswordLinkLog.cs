using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceCredentials.Sql.Logging;

/// <summary>
/// MessageLogging for set-password links (FDW-941). Token values are never logged.
/// </summary>
[MessageLoggingTypeCode("SQL")]
public static partial class PasswordLinkLog
{
    /// <summary>Logged at Information level when a password link is created.</summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Information, Message = "Password link '{linkId}' created for user '{userId}'; earlier unused links for the user were revoked")]
    public static partial IGenericMessage LinkCreated(ILogger logger, Guid userId, Guid linkId);

    /// <summary>Logged at Information level when a password link is used to set a password.</summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Information, Message = "Password link '{linkId}' used for user '{userId}'")]
    public static partial IGenericMessage LinkRedeemed(ILogger logger, Guid userId, Guid linkId);

    /// <summary>Logged at Warning level when a link is created or presented without a token.</summary>
    [MessageLogging(EventId = 21010, Level = LogLevel.Warning, Message = "A password link operation '{operation}' was given no token")]
    public static partial IGenericMessage TokenMissing(ILogger logger, string operation);

    /// <summary>Logged at Warning level when a presented token names no live link.</summary>
    /// <remarks>Unknown, used, revoked and expired are one answer on purpose: which one it was would help a guesser.</remarks>
    [MessageLogging(EventId = 31010, Level = LogLevel.Warning, Message = "A password link was presented that is unknown, already used, revoked or expired")]
    public static partial IGenericMessage LinkRefused(ILogger logger);

    /// <summary>Logged at Error level when the vault name is not configured.</summary>
    [MessageLogging(EventId = 61010, Level = LogLevel.Error, Message = "Password link credential vault name is missing from configuration")]
    public static partial IGenericMessage VaultNameMissing(ILogger logger);

    /// <summary>Logged at Error level when the vault cannot be resolved by name, or is not a password link vault.</summary>
    [MessageLogging(EventId = 61011, Level = LogLevel.Error, Message = "Password link credential vault '{vaultName}' could not be resolved")]
    public static partial IGenericMessage VaultResolveFailed(ILogger logger, string vaultName);
}
