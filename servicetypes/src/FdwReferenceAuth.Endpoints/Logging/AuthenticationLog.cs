using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints.Logging;

/// <summary>
/// MessageLogging class for authentication operations.
/// EventId range: 7000-7199
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class AuthenticationLog
{
    // ========================================================================
    // Token Operations (EventId 7000-7099)
    // ========================================================================

    [MessageLogging(
        EventId = 7001,
        Level = LogLevel.Information,
        Message = "Token generated for user '{username}'")]
    public static partial IGenericMessage TokenGenerated(ILogger logger, string username);

    [MessageLogging(
        EventId = 7002,
        Level = LogLevel.Warning,
        Message = "Authentication failed for user '{username}'")]
    public static partial IGenericMessage AuthenticationFailed(ILogger logger, string username);

    // Why: Debug, not Information — this fires on every authenticated request to a protected
    // endpoint; it is a per-request breadcrumb, not a milestone worth Information-level noise.
    [MessageLogging(
        EventId = 7003,
        Level = LogLevel.Debug,
        Message = "Protected resource accessed by user '{username}'")]
    public static partial IGenericMessage ProtectedResourceAccessed(ILogger logger, string username);

    [MessageLogging(
        EventId = 7004,
        Level = LogLevel.Error,
        Message = "JWT configuration not found")]
    public static partial IGenericMessage JwtConfigurationNotFound(ILogger logger);

    [MessageLogging(
        EventId = 7005,
        Level = LogLevel.Information,
        Message = "User '{username}' logged out")]
    public static partial IGenericMessage UserLoggedOut(ILogger logger, string username);

    [MessageLogging(
        EventId = 7006,
        Level = LogLevel.Warning,
        Message = "Invalid token presented for user '{username}'")]
    public static partial IGenericMessage InvalidToken(ILogger logger, string username);

    [MessageLogging(
        EventId = 7007,
        Level = LogLevel.Warning,
        Message = "Token expired for user '{username}'")]
    public static partial IGenericMessage TokenExpired(ILogger logger, string username);

    // ========================================================================
    // Multi-Tenancy Operations (EventId 7100-7199)
    // ========================================================================

    [MessageLogging(
        EventId = 7100,
        Level = LogLevel.Warning,
        Message = "User '{username}' denied access to tenant '{tenantId}'")]
    public static partial IGenericMessage TenantAccessDenied(ILogger logger, string username, string tenantId);

    [MessageLogging(
        EventId = 7101,
        Level = LogLevel.Information,
        Message = "User '{username}' authenticated for tenant '{tenantId}'")]
    public static partial IGenericMessage TenantAuthenticated(ILogger logger, string username, Guid tenantId);

    [MessageLogging(
        EventId = 7102,
        Level = LogLevel.Error,
        Message = "JWT authentication service not available: {error}")]
    public static partial IGenericMessage JwtAuthenticationServiceNotAvailable(ILogger logger, string error);

    [MessageLogging(
        EventId = 7103,
        Level = LogLevel.Error,
        Message = "Service is not IJwtAuthenticationService")]
    public static partial IGenericMessage InvalidJwtAuthenticationService(ILogger logger);

    // ========================================================================
    // Password Operations (EventId 7110-7119)
    // ========================================================================

    [MessageLogging(
        EventId = 7110,
        Level = LogLevel.Information,
        Message = "Password change requested for user '{username}'")]
    public static partial IGenericMessage PasswordChangeRequested(ILogger logger, string username);

    [MessageLogging(
        EventId = 7111,
        Level = LogLevel.Information,
        Message = "Password changed successfully for user '{username}'")]
    public static partial IGenericMessage PasswordChanged(ILogger logger, string username);

    [MessageLogging(
        EventId = 7112,
        Level = LogLevel.Warning,
        Message = "Password change failed for user '{username}'")]
    public static partial IGenericMessage PasswordChangeFailed(ILogger logger, string username);

    [MessageLogging(
        EventId = 7113,
        Level = LogLevel.Information,
        Message = "Password reset requested for user '{userId}' by admin '{admin}'")]
    public static partial IGenericMessage PasswordResetRequested(ILogger logger, string userId, string admin);

    [MessageLogging(
        EventId = 7114,
        Level = LogLevel.Information,
        Message = "Password reset completed for user '{userId}'")]
    public static partial IGenericMessage PasswordResetCompleted(ILogger logger, string userId);

    // ========================================================================
    // Personal Access Token Operations (EventId 7120-7139)
    // ========================================================================

    [MessageLogging(
        EventId = 7120,
        Level = LogLevel.Information,
        Message = "Creating personal access token for user '{username}'")]
    public static partial IGenericMessage CreatingPersonalAccessToken(ILogger logger, string username);

    [MessageLogging(
        EventId = 7121,
        Level = LogLevel.Information,
        Message = "Personal access token created for user '{username}'")]
    public static partial IGenericMessage PersonalAccessTokenCreated(ILogger logger, string username);

    [MessageLogging(
        EventId = 7122,
        Level = LogLevel.Information,
        Message = "Listing personal access tokens for user '{username}'")]
    public static partial IGenericMessage ListingPersonalAccessTokens(ILogger logger, string username);

    [MessageLogging(
        EventId = 7123,
        Level = LogLevel.Information,
        Message = "Revoking personal access token '{tokenId}' for user '{username}'")]
    public static partial IGenericMessage RevokingPersonalAccessToken(ILogger logger, string tokenId, string username);

    // ========================================================================
    // Agent Key Operations (EventId 7140-7159)
    // ========================================================================

    [MessageLogging(
        EventId = 7140,
        Level = LogLevel.Information,
        Message = "Creating agent key '{label}'")]
    public static partial IGenericMessage CreatingAgentKey(ILogger logger, string label);

    [MessageLogging(
        EventId = 7141,
        Level = LogLevel.Information,
        Message = "Agent key '{label}' created")]
    public static partial IGenericMessage AgentKeyCreated(ILogger logger, string label);

    [MessageLogging(
        EventId = 7142,
        Level = LogLevel.Information,
        Message = "Listing agent keys")]
    public static partial IGenericMessage ListingAgentKeys(ILogger logger);

    [MessageLogging(
        EventId = 7143,
        Level = LogLevel.Information,
        Message = "Deleting agent key '{keyId}'")]
    public static partial IGenericMessage DeletingAgentKey(ILogger logger, string keyId);

    // ========================================================================
    // User Preference Operations (EventId 7160-7179)
    // ========================================================================

    [MessageLogging(
        EventId = 7160,
        Level = LogLevel.Information,
        Message = "Getting preferences for user '{username}'")]
    public static partial IGenericMessage GettingUserPreferences(ILogger logger, string username);

    [MessageLogging(
        EventId = 7161,
        Level = LogLevel.Information,
        Message = "Updating preferences for user '{username}'")]
    public static partial IGenericMessage UpdatingUserPreferences(ILogger logger, string username);

    // ========================================================================
    // Credential Operations (EventId 7180-7199)
    // ========================================================================

    [MessageLogging(
        EventId = 7180,
        Level = LogLevel.Warning,
        Message = "Invalid credentials supplied for user '{username}'")]
    public static partial IGenericMessage InvalidCredentials(ILogger logger, string username);

    // ========================================================================
    // External Identity Provider Discovery (EventId 7200-7219)
    // ========================================================================

    [MessageLogging(
        EventId = 7200,
        Level = LogLevel.Trace,
        Message = "Listing the external login flows this host serves")]
    public static partial IGenericMessage ListingExternalProviders(ILogger logger);

    [MessageLogging(
        EventId = 7201,
        Level = LogLevel.Information,
        Message = "Serving {count} external login flow(s) of {flowCount} configured")]
    public static partial IGenericMessage ExternalProvidersListed(ILogger logger, int count, int flowCount);

    [MessageLogging(
        EventId = 7202,
        Level = LogLevel.Error,
        Message = "Cannot list external login flows -- the configured flows are unreadable: {message}")]
    public static partial IGenericMessage ExternalProviderFlowsUnreadable(ILogger logger, string message);

    [MessageLogging(
        EventId = 7203,
        Level = LogLevel.Error,
        Message = "Flow '{flowName}' federates through step '{stepName}' whose authority cannot be read: {message}")]
    public static partial IGenericMessage ExternalProviderAuthorityUnreadable(
        ILogger logger, string flowName, string stepName, string message);

    // ========================================================================
    // Sign-in flows a page may offer (EventId 7230-7239)
    // ========================================================================

    [MessageLogging(
        EventId = 7230,
        Level = LogLevel.Trace,
        Message = "Listing the sign-in flows that admit {returnUrl}")]
    public static partial IGenericMessage ListingSignInFlows(ILogger logger, string returnUrl);

    [MessageLogging(
        EventId = 7231,
        Level = LogLevel.Information,
        Message = "Offering {count} sign-in flow(s) of {flowCount} configured to {returnUrl}")]
    public static partial IGenericMessage SignInFlowsListed(ILogger logger, int count, int flowCount, string returnUrl);

    [MessageLogging(
        EventId = 7232,
        Level = LogLevel.Debug,
        Message = "Sign-in flow '{flowName}' is not offered to {returnUrl}: {reason}")]
    public static partial IGenericMessage SignInFlowNotOffered(ILogger logger, string flowName, string returnUrl, string reason);

    [MessageLogging(
        EventId = 7233,
        Level = LogLevel.Warning,
        Message = "Cannot list sign-in flows for '{returnUrl}': it is not an absolute URL")]
    public static partial IGenericMessage SignInFlowsReturnUrlInvalid(ILogger logger, string returnUrl);

    [MessageLogging(
        EventId = 7234,
        Level = LogLevel.Error,
        Message = "Cannot list sign-in flows -- the configured flows are unreadable")]
    public static partial IGenericMessage SignInFlowsUnreadable(ILogger logger);

    [MessageLogging(
        EventId = 7235,
        Level = LogLevel.Error,
        Message = "Sign-in flow '{flowName}' delivers into session service '{serviceName}', which cannot be read as a Session service: {reason}")]
    public static partial IGenericMessage SignInFlowSessionServiceUnreadable(
        ILogger logger, string flowName, string serviceName, string reason);

    [MessageLogging(
        EventId = 7236,
        Level = LogLevel.Error,
        Message = "Sign-in flow '{flowName}' federates through step '{stepName}' whose authority cannot be read: {reason}")]
    public static partial IGenericMessage SignInFlowAuthorityUnreadable(
        ILogger logger, string flowName, string stepName, string reason);

    // ========================================================================
    // Redirect sign-in completion: return URL, one-time code, redeem (EventId 7220-7229)
    // ========================================================================

    [MessageLogging(
        EventId = 7220,
        Level = LogLevel.Warning,
        Message = "Flow '{flowName}' refused a return URL that is not on its allow-list")]
    public static partial IGenericMessage ReturnUrlNotAllowed(ILogger logger, string flowName);

    [MessageLogging(
        EventId = 7221,
        Level = LogLevel.Warning,
        Message = "The sign-in callback arrived without the return target cookies set by /auth/oidc/redirect")]
    public static partial IGenericMessage CallbackReturnTargetMissing(ILogger logger);

    [MessageLogging(
        EventId = 7222,
        Level = LogLevel.Warning,
        Message = "The sign-in callback for flow '{flowName}' arrived without a resume cookie")]
    public static partial IGenericMessage CallbackResumeMissing(ILogger logger, string flowName);

    [MessageLogging(
        EventId = 7223,
        Level = LogLevel.Warning,
        Message = "The sign-in for flow '{flowName}' did not complete; returning the browser with an error")]
    public static partial IGenericMessage CallbackSignInFailed(ILogger logger, string flowName);

    [MessageLogging(
        EventId = 7224,
        Level = LogLevel.Error,
        Message = "The sign-in for flow '{flowName}' completed but no one-time code could be issued")]
    public static partial IGenericMessage CallbackCodeNotIssued(ILogger logger, string flowName);

    /// <summary>A redeemed code carried a session whose cookie could not be set (FDW-903).</summary>
    [MessageLogging(
        EventId = 71600,
        Level = LogLevel.Error,
        Message = "A code from flow '{flowName}' was redeemed but its session cookie could not be set")]
    public static partial IGenericMessage RedeemSessionNotDelivered(ILogger logger, string flowName);

    /// <summary>A CSRF token was handed to a session's browser app (FDW-903).</summary>
    [MessageLogging(
        EventId = 11600,
        Level = LogLevel.Debug,
        Message = "CSRF token issued to the session behind this request")]
    public static partial IGenericMessage SessionCsrfTokenIssued(ILogger logger);

    /// <summary>A CSRF token was asked for but could not be issued (FDW-903).</summary>
    [MessageLogging(
        EventId = 21600,
        Level = LogLevel.Warning,
        Message = "No CSRF token could be issued: the request was not authenticated by a live server-side session")]
    public static partial IGenericMessage SessionCsrfTokenRefused(ILogger logger);

    [MessageLogging(
        EventId = 7225,
        Level = LogLevel.Trace,
        Message = "The sign-in for flow '{flowName}' completed; returning the browser with a one-time code")]
    public static partial IGenericMessage CallbackCompleted(ILogger logger, string flowName);

    [MessageLogging(
        EventId = 7226,
        Level = LogLevel.Warning,
        Message = "A one-time sign-in code was not accepted")]
    public static partial IGenericMessage RedeemRefused(ILogger logger);

    [MessageLogging(
        EventId = 7227,
        Level = LogLevel.Trace,
        Message = "A one-time sign-in code was redeemed")]
    public static partial IGenericMessage Redeemed(ILogger logger);

    [MessageLogging(
        EventId = 7240,
        Level = LogLevel.Information,
        Message = "A password link set the password of user '{userId}' (FDW-941)")]
    public static partial IGenericMessage PasswordLinkUsed(ILogger logger, Guid userId);

    [MessageLogging(
        EventId = 7241,
        Level = LogLevel.Warning,
        Message = "A password link was refused: unknown, already used, revoked or expired (FDW-941)")]
    public static partial IGenericMessage PasswordLinkRefused(ILogger logger);

    [MessageLogging(
        EventId = 7242,
        Level = LogLevel.Error,
        Message = "A password link for user '{userId}' was used up but the new password could not be stored; the user needs a new link (FDW-941)")]
    public static partial IGenericMessage PasswordLinkPasswordNotStored(ILogger logger, Guid userId);

    [MessageLogging(
        EventId = 7243,
        Level = LogLevel.Error,
        Message = "A password link could not be checked: the password link service failed (FDW-941)")]
    public static partial IGenericMessage PasswordLinkNotChecked(ILogger logger);
}
