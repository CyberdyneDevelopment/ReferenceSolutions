using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Management.UI.Tailwind.Logging;

/// <summary>
/// Structured logging for authentication operations.
/// EventId range: 9501-9599
/// </summary>
public static partial class AuthLog
{
    /// <summary>Logs a login attempt for the specified user.</summary>
    [MessageLogging(EventId = 9501, Level = LogLevel.Information, Message = "Login attempt for user {username}")]
    public static partial IGenericMessage LoginAttempt(ILogger logger, string username);

    /// <summary>Logs a successful login for the specified user.</summary>
    [MessageLogging(EventId = 9502, Level = LogLevel.Information, Message = "Login succeeded for user {username}")]
    public static partial IGenericMessage LoginSucceeded(ILogger logger, string username);

    /// <summary>Logs a failed login attempt with the server status code.</summary>
    [MessageLogging(EventId = 9503, Level = LogLevel.Warning, Message = "Login failed for user {username} with status {statusCode}")]
    public static partial IGenericMessage LoginFailed(ILogger logger, string username, int statusCode);

    /// <summary>Logs an exception during login.</summary>
    [MessageLogging(EventId = 9504, Level = LogLevel.Error, Message = "Login exception for user {username}")]
    public static partial IGenericMessage LoginException(ILogger logger, Exception ex, string username);

    /// <summary>Logs a successful logout for the specified user.</summary>
    [MessageLogging(EventId = 9505, Level = LogLevel.Information, Message = "Logout succeeded for user {username}")]
    public static partial IGenericMessage LogoutSucceeded(ILogger logger, string username);

    /// <summary>Logs a token refresh attempt.</summary>
    [MessageLogging(EventId = 9506, Level = LogLevel.Debug, Message = "Token refresh attempt")]
    public static partial IGenericMessage TokenRefreshAttempt(ILogger logger);

    /// <summary>Logs a successful token refresh.</summary>
    [MessageLogging(EventId = 9507, Level = LogLevel.Debug, Message = "Token refresh succeeded")]
    public static partial IGenericMessage TokenRefreshSucceeded(ILogger logger);

    /// <summary>Logs a failed token refresh with the server status code.</summary>
    [MessageLogging(EventId = 9508, Level = LogLevel.Warning, Message = "Token refresh failed with status {statusCode}")]
    public static partial IGenericMessage TokenRefreshFailed(ILogger logger, int statusCode);

    /// <summary>Logs an exception during token refresh.</summary>
    [MessageLogging(EventId = 9509, Level = LogLevel.Error, Message = "Token refresh exception")]
    public static partial IGenericMessage TokenRefreshException(ILogger logger, Exception ex);

    /// <summary>Logs that token refresh requires re-authentication.</summary>
    [MessageLogging(EventId = 9510, Level = LogLevel.Warning, Message = "Token refresh requires re-authentication")]
    public static partial IGenericMessage TokenRefreshRequiresReauth(ILogger logger);

    /// <summary>Logs that tokens were stored with the given expiration.</summary>
    [MessageLogging(EventId = 9511, Level = LogLevel.Debug, Message = "Tokens stored, expires at {expiration}")]
    public static partial IGenericMessage TokensStored(ILogger logger, long expiration);

    /// <summary>Logs that tokens were cleared from storage.</summary>
    [MessageLogging(EventId = 9512, Level = LogLevel.Debug, Message = "Tokens cleared")]
    public static partial IGenericMessage TokensCleared(ILogger logger);

    /// <summary>Logs that auth state was restored for the specified user.</summary>
    [MessageLogging(EventId = 9513, Level = LogLevel.Debug, Message = "Auth state restored for {username}")]
    public static partial IGenericMessage AuthStateRestored(ILogger logger, string username);

    /// <summary>Logs an auth state change.</summary>
    [MessageLogging(EventId = 9514, Level = LogLevel.Debug, Message = "Auth state changed: IsAuthenticated={isAuthenticated}")]
    public static partial IGenericMessage AuthStateChanged(ILogger logger, bool isAuthenticated);

    /// <summary>Logs that auth state restoration was deferred because JS interop is not ready.</summary>
    [MessageLogging(EventId = 9515, Level = LogLevel.Debug, Message = "Auth state restoration deferred - JS interop not ready")]
    public static partial IGenericMessage AuthStateRestorationDeferred(ILogger logger);

    /// <summary>Logs a login failure with an empty server response.</summary>
    [MessageLogging(EventId = 9518, Level = LogLevel.Warning, Message = "Login failed for user {username}: server returned {statusCode} with no content (authentication may not be configured on server)")]
    public static partial IGenericMessage LoginEmptyResponse(ILogger logger, string username, int statusCode);

    /// <summary>Logs a login failure due to an invalid response format.</summary>
    [MessageLogging(EventId = 9519, Level = LogLevel.Warning, Message = "Login failed for user {username}: invalid response format from server")]
    public static partial IGenericMessage LoginInvalidResponse(ILogger logger, string username);

    /// <summary>Logs a 401 response triggering a token refresh attempt.</summary>
    [MessageLogging(EventId = 9526, Level = LogLevel.Debug, Message = "Received 401, attempting token refresh")]
    public static partial IGenericMessage UnauthorizedRefreshAttempt(ILogger logger);

    /// <summary>Logs a failure during server-side logout (best-effort).</summary>
    [MessageLogging(EventId = 9528, Level = LogLevel.Warning, Message = "Server logout best-effort failed")]
    public static partial IGenericMessage LogoutFailed(ILogger logger, Exception ex);
}
