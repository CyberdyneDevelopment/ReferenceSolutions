using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace ReferenceMultitenancy.Sql.Logging;

/// <summary>
/// MessageLogging for tenant resolution middleware operations.
/// EventId range: 560-569
/// </summary>
[MessageLoggingTypeCode("SQLTENANT")]
public static partial class TenantMiddlewareLog
{
    /// <summary>Logs tenant resolved from JWT claim.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug, Message = "Resolved tenant '{tenantId}' from JWT claim")]
    public static partial IGenericMessage ResolvedTenantFromJwtClaim(ILogger logger, Guid tenantId);

    /// <summary>Logs tenant from JWT claim not found.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning, Message = "Tenant '{tenantId}' from JWT claim not found")]
    public static partial IGenericMessage TenantFromJwtClaimNotFound(ILogger logger, Guid tenantId);

    /// <summary>Logs tenant resolved from X-Tenant-Id header.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug, Message = "Resolved tenant '{tenantId}' from X-Tenant-Id header")]
    public static partial IGenericMessage ResolvedTenantFromHeader(ILogger logger, Guid tenantId);

    /// <summary>Logs tenant resolved by slug from X-Tenant-Id header.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug, Message = "Resolved tenant by slug '{slug}' from X-Tenant-Id header")]
    public static partial IGenericMessage ResolvedTenantBySlug(ILogger logger, string slug);

    /// <summary>Logs access denied when user does not have access to the requested tenant via header.</summary>
    [MessageLogging(EventId = 51000, Level = LogLevel.Warning, Message = "User '{userId}' denied access to tenant '{tenantId}' via X-Tenant-Id header")]
    public static partial IGenericMessage TenantHeaderAccessDenied(ILogger logger, string userId, Guid tenantId);

    /// <summary>Logs when tenant access validation query fails (infrastructure error).</summary>
    [MessageLogging(EventId = 51001, Level = LogLevel.Error, Message = "Tenant access check failed for user '{userId}' on tenant '{tenantId}'")]
    public static partial IGenericMessage TenantAccessCheckFailed(ILogger logger, string userId, Guid tenantId);

    /// <summary>Logs when using tenant-specific connection key.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace, Message = "Using tenant '{tenantSlug}' connection key '{connectionKey}'")]
    public static partial IGenericMessage UsingTenantConnectionKey(ILogger logger, string tenantSlug, string connectionKey);

    /// <summary>Logs when using default connection key (no tenant override).</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace, Message = "Using default configuration connection '{connectionName}'")]
    public static partial IGenericMessage UsingDefaultConnectionKey(ILogger logger, string connectionName);

    // -- Org resolution (EventId range 570-579) --

    /// <summary>Logs org resolved from JWT org_id claim.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Debug, Message = "Resolved org '{orgId}' from JWT org_id claim")]
    public static partial IGenericMessage ResolvedOrgFromJwtClaim(ILogger logger, Guid orgId);

    /// <summary>Logs org from JWT org_id claim not found in the database.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning, Message = "Org '{orgId}' from JWT org_id claim not found")]
    public static partial IGenericMessage OrgFromJwtClaimNotFound(ILogger logger, Guid orgId);

    /// <summary>Logs org resolved from X-Org-Id header.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Debug, Message = "Resolved org '{orgId}' from X-Org-Id header")]
    public static partial IGenericMessage ResolvedOrgFromHeader(ILogger logger, Guid orgId);

    /// <summary>Logs org from X-Org-Id header not found.</summary>
    [MessageLogging(EventId = 31002, Level = LogLevel.Warning, Message = "Org '{orgId}' from X-Org-Id header not found")]
    public static partial IGenericMessage OrgFromHeaderNotFound(ILogger logger, Guid orgId);

    /// <summary>Logs org resolved from the tenant's default org (IsDefault=1 fallback).</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Debug, Message = "Resolved org '{orgId}' as default org for tenant '{tenantId}'")]
    public static partial IGenericMessage ResolvedDefaultOrg(ILogger logger, Guid orgId, Guid tenantId);

    /// <summary>Logs when no default org is configured for the current tenant — org context not set.</summary>
    [MessageLogging(EventId = 31003, Level = LogLevel.Warning, Message = "No default org found for tenant '{tenantId}' — org context not set")]
    public static partial IGenericMessage NoDefaultOrgForTenant(ILogger logger, Guid tenantId);

    /// <summary>Logs when org resolution is skipped because there is no active tenant context.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace, Message = "Org resolution skipped — no tenant context on this request")]
    public static partial IGenericMessage OrgResolutionSkippedNoTenant(ILogger logger);

    /// <summary>Logs when ConfigurationConnectionOptions.ConnectionName is not set — configuration is missing.</summary>
    [MessageLogging(EventId = 61000, Level = LogLevel.Error, Message = "ConfigurationConnectionOptions.ConnectionName is not configured. Ensure the FdwHost:Configuration:ConnectionName setting is present.")]
    public static partial IGenericMessage ConfigurationConnectionNameMissing(ILogger logger);
}
