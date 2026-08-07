using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging definitions for Tenant operations.
/// EventId range: 8600-8649
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class TenantLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Access control operations (8600-8619)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8600, Level = LogLevel.Debug, Message = "Filtering query by tenant '{tenantId}'")]
    public static partial IGenericMessage FilteringByTenant(ILogger logger, Guid tenantId);

    [MessageLogging(EventId = 8601, Level = LogLevel.Debug, Message = "System admin bypassing tenant filter for query")]
    public static partial IGenericMessage AdminBypassingTenantFilter(ILogger logger);

    [MessageLogging(EventId = 8602, Level = LogLevel.Warning, Message = "Access denied: User '{username}' attempted to access resource in tenant '{tenantId}'")]
    public static partial IGenericMessage TenantAccessDenied(ILogger logger, string username, Guid tenantId);

    [MessageLogging(EventId = 8603, Level = LogLevel.Warning, Message = "No tenant context available for request")]
    public static partial IGenericMessage NoTenantContext(ILogger logger);

    [MessageLogging(EventId = 8604, Level = LogLevel.Information, Message = "Creating resource for tenant '{tenantId}'")]
    public static partial IGenericMessage CreatingResourceForTenant(ILogger logger, Guid tenantId);

    [MessageLogging(EventId = 8605, Level = LogLevel.Information, Message = "Creating system resource (no tenant)")]
    public static partial IGenericMessage CreatingSystemResource(ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Validation operations (8620-8639)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8620, Level = LogLevel.Warning, Message = "Resource '{resourceName}' belongs to different tenant '{resourceTenantId}', current tenant is '{currentTenantId}'")]
    public static partial IGenericMessage TenantMismatch(ILogger logger, string resourceName, Guid resourceTenantId, Guid currentTenantId);

    [MessageLogging(EventId = 8621, Level = LogLevel.Warning, Message = "Cannot modify system resource '{resourceName}' without admin privileges")]
    public static partial IGenericMessage SystemResourceModificationDenied(ILogger logger, string resourceName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Tenant management operations (8630-8639)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8630, Level = LogLevel.Information, Message = "User '{userId}' listing tenants (Admin: {isAdmin}, IncludeInactive: {includeInactive})")]
    public static partial IGenericMessage ListingTenants(ILogger logger, string userId, bool isAdmin, bool includeInactive);

    [MessageLogging(EventId = 8631, Level = LogLevel.Warning, Message = "Failed to get tenants: {message}")]
    public static partial IGenericMessage GetTenantsFailed(ILogger logger, string message);

    [MessageLogging(EventId = 8632, Level = LogLevel.Warning, Message = "User '{userId}' denied access to tenant '{tenantId}'")]
    public static partial IGenericMessage UserTenantAccessDenied(ILogger logger, string userId, Guid tenantId);

    [MessageLogging(EventId = 8633, Level = LogLevel.Warning, Message = "User '{userId}' denied tenant switch to '{tenantId}'")]
    public static partial IGenericMessage TenantSwitchDenied(ILogger logger, string userId, Guid tenantId);

    [MessageLogging(EventId = 8634, Level = LogLevel.Information, Message = "User '{userId}' switched to tenant '{tenantId}'")]
    public static partial IGenericMessage TenantSwitched(ILogger logger, string userId, Guid tenantId);

    [MessageLogging(EventId = 8635, Level = LogLevel.Error, Message = "JWT configuration not found — cannot generate tenant switch token")]
    public static partial IGenericMessage JwtConfigurationNotFound(ILogger logger);

    // Why: EventId 8636 reserved for TenantSwitchNotSupported — tenant-scoped tokens require
    // a new /connect/token request from the client; the server cannot mint them externally
    // with OpenIddict RS256 tokens (no hand-mint path exists post auth-cutover).
    [MessageLogging(EventId = 8636, Level = LogLevel.Warning, Message = "Tenant switch token mint not supported post auth-cutover: user '{userId}' tenant '{tenantId}' — client must re-authenticate via /connect/token")]
    public static partial IGenericMessage TenantSwitchNotSupported(ILogger logger, string userId, Guid tenantId);

    [MessageLogging(EventId = 8637, Level = LogLevel.Warning, Message = "User '{userId}' is not a member of tenant '{tenantId}'; cannot set as default")]
    public static partial IGenericMessage SetDefaultTenantNotMember(ILogger logger, string userId, Guid tenantId);

    [MessageLogging(EventId = 8638, Level = LogLevel.Error, Message = "Failed to set default tenant '{tenantId}' for user '{userId}'")]
    public static partial IGenericMessage SetDefaultTenantFailed(ILogger logger, Exception ex, string userId, Guid tenantId);

    [MessageLogging(EventId = 8639, Level = LogLevel.Information, Message = "User '{userId}' set tenant '{tenantId}' as default")]
    public static partial IGenericMessage DefaultTenantSet(ILogger logger, string userId, Guid tenantId);
}
