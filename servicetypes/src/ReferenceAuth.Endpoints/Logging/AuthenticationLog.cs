using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace ReferenceAuth.Endpoints.Logging;

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
}
