using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace ReferenceSettings.Endpoints.Logging;

/// <summary>
/// MessageLogging definitions for Settings management endpoints.
/// EventId range: 9100-9149
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class SettingsLog
{
    // Server setting create (9100-9104)
    [MessageLogging(EventId = 9100, Level = LogLevel.Debug, Message = "Creating server setting '{name}'")]
    public static partial IGenericMessage CreatingServerSetting(ILogger logger, string name);

    [MessageLogging(EventId = 9101, Level = LogLevel.Information, Message = "Server setting '{name}' created successfully")]
    public static partial IGenericMessage ServerSettingCreated(ILogger logger, string name);

    [MessageLogging(EventId = 9102, Level = LogLevel.Warning, Message = "Server setting '{name}' already exists")]
    public static partial IGenericMessage ServerSettingAlreadyExists(ILogger logger, string name);

    [MessageLogging(EventId = 9103, Level = LogLevel.Error, Message = "Failed to create server setting '{name}': {message}")]
    public static partial IGenericMessage ServerSettingCreateFailed(ILogger logger, string name, string message);

    // Server setting read (9105-9107)
    [MessageLogging(EventId = 9105, Level = LogLevel.Trace, Message = "Getting server setting '{name}'")]
    public static partial IGenericMessage GettingServerSetting(ILogger logger, string name);

    [MessageLogging(EventId = 9106, Level = LogLevel.Information, Message = "Server setting '{name}' retrieved")]
    public static partial IGenericMessage ServerSettingRetrieved(ILogger logger, string name);

    [MessageLogging(EventId = 9107, Level = LogLevel.Warning, Message = "Server setting '{name}' not found")]
    public static partial IGenericMessage ServerSettingNotFound(ILogger logger, string name);

    // Server setting list (9108-9109)
    [MessageLogging(EventId = 9108, Level = LogLevel.Trace, Message = "Listing server settings")]
    public static partial IGenericMessage ListingServerSettings(ILogger logger);

    [MessageLogging(EventId = 9109, Level = LogLevel.Information, Message = "Listed {count} server settings")]
    public static partial IGenericMessage ServerSettingsListed(ILogger logger, int count);

    // Server setting update (9110-9113)
    [MessageLogging(EventId = 9110, Level = LogLevel.Debug, Message = "Updating server setting '{name}'")]
    public static partial IGenericMessage UpdatingServerSetting(ILogger logger, string name);

    [MessageLogging(EventId = 9111, Level = LogLevel.Information, Message = "Server setting '{name}' updated successfully")]
    public static partial IGenericMessage ServerSettingUpdated(ILogger logger, string name);

    [MessageLogging(EventId = 9112, Level = LogLevel.Warning, Message = "Server setting '{name}' not found for update")]
    public static partial IGenericMessage ServerSettingNotFoundForUpdate(ILogger logger, string name);

    [MessageLogging(EventId = 9113, Level = LogLevel.Error, Message = "Failed to update server setting '{name}': {message}")]
    public static partial IGenericMessage ServerSettingUpdateFailed(ILogger logger, string name, string message);

    // Role setting create (9115-9118)
    [MessageLogging(EventId = 9115, Level = LogLevel.Debug, Message = "Creating role setting '{name}' for role '{roleName}'")]
    public static partial IGenericMessage CreatingRoleSetting(ILogger logger, string name, string roleName);

    [MessageLogging(EventId = 9116, Level = LogLevel.Information, Message = "Role setting '{name}' for role '{roleName}' created successfully")]
    public static partial IGenericMessage RoleSettingCreated(ILogger logger, string name, string roleName);

    [MessageLogging(EventId = 9117, Level = LogLevel.Warning, Message = "Role setting '{name}' for role '{roleName}' already exists")]
    public static partial IGenericMessage RoleSettingAlreadyExists(ILogger logger, string name, string roleName);

    [MessageLogging(EventId = 9118, Level = LogLevel.Error, Message = "Failed to create role setting '{name}' for role '{roleName}': {message}")]
    public static partial IGenericMessage RoleSettingCreateFailed(ILogger logger, string name, string roleName, string message);

    // Role setting list (9119-9120)
    [MessageLogging(EventId = 9119, Level = LogLevel.Trace, Message = "Listing role settings")]
    public static partial IGenericMessage ListingRoleSettings(ILogger logger);

    [MessageLogging(EventId = 9120, Level = LogLevel.Information, Message = "Listed {count} role settings")]
    public static partial IGenericMessage RoleSettingsListed(ILogger logger, int count);

    // Role setting update (9121-9124)
    [MessageLogging(EventId = 9121, Level = LogLevel.Debug, Message = "Updating role setting '{name}' for role '{roleName}'")]
    public static partial IGenericMessage UpdatingRoleSetting(ILogger logger, string name, string roleName);

    [MessageLogging(EventId = 9122, Level = LogLevel.Information, Message = "Role setting '{name}' for role '{roleName}' updated successfully")]
    public static partial IGenericMessage RoleSettingUpdated(ILogger logger, string name, string roleName);

    [MessageLogging(EventId = 9123, Level = LogLevel.Warning, Message = "Role setting '{name}' for role '{roleName}' not found for update")]
    public static partial IGenericMessage RoleSettingNotFoundForUpdate(ILogger logger, string name, string roleName);

    [MessageLogging(EventId = 9124, Level = LogLevel.Error, Message = "Failed to update role setting '{name}' for role '{roleName}': {message}")]
    public static partial IGenericMessage RoleSettingUpdateFailed(ILogger logger, string name, string roleName, string message);

    // Tenant setting create (9125-9128)
    [MessageLogging(EventId = 9125, Level = LogLevel.Debug, Message = "Creating tenant setting '{name}'")]
    public static partial IGenericMessage CreatingTenantSetting(ILogger logger, string name);

    [MessageLogging(EventId = 9126, Level = LogLevel.Information, Message = "Tenant setting '{name}' created successfully")]
    public static partial IGenericMessage TenantSettingCreated(ILogger logger, string name);

    [MessageLogging(EventId = 9127, Level = LogLevel.Warning, Message = "Tenant setting '{name}' already exists")]
    public static partial IGenericMessage TenantSettingAlreadyExists(ILogger logger, string name);

    [MessageLogging(EventId = 9128, Level = LogLevel.Error, Message = "Failed to create tenant setting '{name}': {message}")]
    public static partial IGenericMessage TenantSettingCreateFailed(ILogger logger, string name, string message);

    // Tenant setting list (9129-9130)
    [MessageLogging(EventId = 9129, Level = LogLevel.Trace, Message = "Listing tenant settings")]
    public static partial IGenericMessage ListingTenantSettings(ILogger logger);

    [MessageLogging(EventId = 9130, Level = LogLevel.Information, Message = "Listed {count} tenant settings")]
    public static partial IGenericMessage TenantSettingsListed(ILogger logger, int count);

    // Tenant setting update (9131-9134)
    [MessageLogging(EventId = 9131, Level = LogLevel.Debug, Message = "Updating tenant setting '{name}'")]
    public static partial IGenericMessage UpdatingTenantSetting(ILogger logger, string name);

    [MessageLogging(EventId = 9132, Level = LogLevel.Information, Message = "Tenant setting '{name}' updated successfully")]
    public static partial IGenericMessage TenantSettingUpdated(ILogger logger, string name);

    [MessageLogging(EventId = 9133, Level = LogLevel.Warning, Message = "Tenant setting '{name}' not found for update")]
    public static partial IGenericMessage TenantSettingNotFoundForUpdate(ILogger logger, string name);

    [MessageLogging(EventId = 9134, Level = LogLevel.Error, Message = "Failed to update tenant setting '{name}': {message}")]
    public static partial IGenericMessage TenantSettingUpdateFailed(ILogger logger, string name, string message);

    // General (9149)
    [MessageLogging(EventId = 9149, Level = LogLevel.Error, Message = "Setting operation '{operation}' failed: {message}")]
    public static partial IGenericMessage SettingOperationFailed(ILogger logger, string operation, string message);
}
