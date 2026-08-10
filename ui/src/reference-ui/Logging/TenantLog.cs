using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Ui.Logging;

/// <summary>
/// Structured logging for tenant UI operations.
/// EventId range: 9800-9849
/// </summary>
public static partial class TenantLog
{
    /// <summary>Logs that the profile dropdown is loading tenant list.</summary>
    [MessageLogging(EventId = 9800, Level = LogLevel.Trace, Message = "Loading tenant list for profile dropdown")]
    public static partial IGenericMessage LoadingTenants(ILogger logger);

    /// <summary>Logs the number of tenants loaded.</summary>
    [MessageLogging(EventId = 9801, Level = LogLevel.Information, Message = "Loaded {count} tenants for profile dropdown")]
    public static partial IGenericMessage LoadedTenants(ILogger logger, int count);

    /// <summary>Logs a failure to load tenants.</summary>
    [MessageLogging(EventId = 9802, Level = LogLevel.Error, Message = "Failed to load tenants for profile dropdown")]
    public static partial IGenericMessage LoadTenantsFailed(ILogger logger, Exception ex);

    /// <summary>Logs the start of a tenant switch.</summary>
    [MessageLogging(EventId = 9803, Level = LogLevel.Trace, Message = "Switching active tenant to {tenantId}")]
    public static partial IGenericMessage SwitchingTenant(ILogger logger, string tenantId);

    /// <summary>Logs a successful tenant switch.</summary>
    [MessageLogging(EventId = 9804, Level = LogLevel.Information, Message = "Tenant switch succeeded for {tenantId}")]
    public static partial IGenericMessage TenantSwitchSucceeded(ILogger logger, string tenantId);

    /// <summary>Logs a failed tenant switch.</summary>
    [MessageLogging(EventId = 9805, Level = LogLevel.Error, Message = "Tenant switch failed for {tenantId}")]
    public static partial IGenericMessage TenantSwitchFailed(ILogger logger, Exception ex, string tenantId);

    /// <summary>Logs the start of a set-default-tenant operation.</summary>
    [MessageLogging(EventId = 9806, Level = LogLevel.Trace, Message = "Setting default tenant to {tenantId}")]
    public static partial IGenericMessage SettingDefaultTenant(ILogger logger, string tenantId);

    /// <summary>Logs a successful set-default-tenant operation.</summary>
    [MessageLogging(EventId = 9807, Level = LogLevel.Information, Message = "Default tenant set to {tenantId}")]
    public static partial IGenericMessage DefaultTenantSet(ILogger logger, string tenantId);

    /// <summary>Logs a failure to set the default tenant.</summary>
    [MessageLogging(EventId = 9808, Level = LogLevel.Error, Message = "Failed to set default tenant to {tenantId}")]
    public static partial IGenericMessage SetDefaultTenantFailed(ILogger logger, Exception ex, string tenantId);

    /// <summary>Logs that re-authentication after tenant switch failed.</summary>
    [MessageLogging(EventId = 9809, Level = LogLevel.Error, Message = "Re-authentication after tenant switch failed: missing tokens in response")]
    public static partial IGenericMessage TenantSwitchReauthFailed(ILogger logger);

    /// <summary>Logs that cross-tenant mode switch succeeded.</summary>
    [MessageLogging(EventId = 9810, Level = LogLevel.Information, Message = "Switched to cross-tenant mode")]
    public static partial IGenericMessage CrossTenantModeEntered(ILogger logger);

    /// <summary>Logs a failure to switch to cross-tenant mode.</summary>
    [MessageLogging(EventId = 9811, Level = LogLevel.Error, Message = "Failed to enter cross-tenant mode")]
    public static partial IGenericMessage CrossTenantModeFailed(ILogger logger, Exception ex);
}
