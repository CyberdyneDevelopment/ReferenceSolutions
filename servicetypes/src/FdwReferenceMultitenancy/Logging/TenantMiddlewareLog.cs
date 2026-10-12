using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceMultitenancy.Logging;

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

    // -- Org resolution (EventId range 570-579) --

    /// <summary>Logs org resolved from JWT org_id claim.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Debug, Message = "Resolved org '{orgId}' from JWT org_id claim")]
    public static partial IGenericMessage ResolvedOrgFromJwtClaim(ILogger logger, Guid orgId);

    /// <summary>Logs org from JWT org_id claim not found in the database.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning, Message = "Org '{orgId}' from JWT org_id claim not found")]
    public static partial IGenericMessage OrgFromJwtClaimNotFound(ILogger logger, Guid orgId);

    /// <summary>Logs org resolved from the tenant's default org (IsDefault=1 fallback).</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Debug, Message = "Resolved org '{orgId}' as default org for tenant '{tenantId}'")]
    public static partial IGenericMessage ResolvedDefaultOrg(ILogger logger, Guid orgId, Guid tenantId);

    /// <summary>Logs when no default org is configured for the current tenant — org context not set.</summary>
    [MessageLogging(EventId = 31003, Level = LogLevel.Warning, Message = "No default org found for tenant '{tenantId}' — org context not set")]
    public static partial IGenericMessage NoDefaultOrgForTenant(ILogger logger, Guid tenantId);

    /// <summary>Logs when org resolution is skipped because there is no active tenant context.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace, Message = "Org resolution skipped — no tenant context on this request")]
    public static partial IGenericMessage OrgResolutionSkippedNoTenant(ILogger logger);
}
