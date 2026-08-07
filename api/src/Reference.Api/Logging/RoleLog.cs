using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging definitions for Role management endpoints.
/// EventId range: 9150-9199
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class RoleLog
{
    // Create role (9150-9153)
    [MessageLogging(EventId = 9150, Level = LogLevel.Debug, Message = "Creating role '{name}'")]
    public static partial IGenericMessage CreatingRole(ILogger logger, string name);

    [MessageLogging(EventId = 9151, Level = LogLevel.Information, Message = "Role '{name}' created with ID '{roleId}'")]
    public static partial IGenericMessage RoleCreated(ILogger logger, string name, Guid roleId);

    [MessageLogging(EventId = 9152, Level = LogLevel.Warning, Message = "Role '{name}' already exists")]
    public static partial IGenericMessage RoleAlreadyExists(ILogger logger, string name);

    [MessageLogging(EventId = 9153, Level = LogLevel.Error, Message = "Failed to create role '{name}': {message}")]
    public static partial IGenericMessage RoleCreateFailed(ILogger logger, string name, string message);

    // Get role (9154-9156)
    [MessageLogging(EventId = 9154, Level = LogLevel.Trace, Message = "Getting role '{name}'")]
    public static partial IGenericMessage GettingRole(ILogger logger, string name);

    [MessageLogging(EventId = 9155, Level = LogLevel.Information, Message = "Role '{name}' retrieved")]
    public static partial IGenericMessage RoleRetrieved(ILogger logger, string name);

    [MessageLogging(EventId = 9156, Level = LogLevel.Warning, Message = "Role '{name}' not found")]
    public static partial IGenericMessage RoleNotFound(ILogger logger, string name);

    // List roles (9157-9158)
    [MessageLogging(EventId = 9157, Level = LogLevel.Trace, Message = "Listing roles")]
    public static partial IGenericMessage ListingRoles(ILogger logger);

    [MessageLogging(EventId = 9158, Level = LogLevel.Information, Message = "Listed {count} roles")]
    public static partial IGenericMessage RolesListed(ILogger logger, int count);

    // Update role (9159-9162)
    [MessageLogging(EventId = 9159, Level = LogLevel.Debug, Message = "Updating role '{name}'")]
    public static partial IGenericMessage UpdatingRole(ILogger logger, string name);

    [MessageLogging(EventId = 9160, Level = LogLevel.Information, Message = "Role '{name}' updated successfully")]
    public static partial IGenericMessage RoleUpdated(ILogger logger, string name);

    [MessageLogging(EventId = 9161, Level = LogLevel.Warning, Message = "Role '{name}' not found for update")]
    public static partial IGenericMessage RoleNotFoundForUpdate(ILogger logger, string name);

    [MessageLogging(EventId = 9162, Level = LogLevel.Error, Message = "Failed to update role '{name}': {message}")]
    public static partial IGenericMessage RoleUpdateFailed(ILogger logger, string name, string message);

    // Delete role (9163-9166)
    [MessageLogging(EventId = 9163, Level = LogLevel.Debug, Message = "Deleting role '{name}' (ID: {roleId})")]
    public static partial IGenericMessage DeletingRole(ILogger logger, string name, Guid roleId);

    [MessageLogging(EventId = 9164, Level = LogLevel.Information, Message = "Role '{name}' deleted successfully")]
    public static partial IGenericMessage RoleDeleted(ILogger logger, string name);

    [MessageLogging(EventId = 9165, Level = LogLevel.Warning, Message = "Role '{name}' not found for delete")]
    public static partial IGenericMessage RoleNotFoundForDelete(ILogger logger, string name);

    [MessageLogging(EventId = 9166, Level = LogLevel.Error, Message = "Failed to delete role '{name}': {message}")]
    public static partial IGenericMessage RoleDeleteFailed(ILogger logger, string name, string message);

    // Get role permissions (9167-9169)
    [MessageLogging(EventId = 9167, Level = LogLevel.Debug, Message = "Getting permissions for role '{name}'")]
    public static partial IGenericMessage GettingRolePermissions(ILogger logger, string name);

    [MessageLogging(EventId = 9168, Level = LogLevel.Information, Message = "Retrieved {count} permissions for role '{name}'")]
    public static partial IGenericMessage RolePermissionsRetrieved(ILogger logger, int count, string name);

    [MessageLogging(EventId = 9169, Level = LogLevel.Error, Message = "Failed to get permissions for role '{name}': {message}")]
    public static partial IGenericMessage RolePermissionsGetFailed(ILogger logger, string name, string message);

    // Set role permissions (9170-9172)
    [MessageLogging(EventId = 9170, Level = LogLevel.Debug, Message = "Setting {count} permissions for role '{name}'")]
    public static partial IGenericMessage SettingRolePermissions(ILogger logger, string name, int count);

    [MessageLogging(EventId = 9171, Level = LogLevel.Information, Message = "Permissions for role '{name}' updated with {count} entries")]
    public static partial IGenericMessage RolePermissionsSet(ILogger logger, string name, int count);

    [MessageLogging(EventId = 9172, Level = LogLevel.Error, Message = "Failed to set permissions for role '{name}': {message}")]
    public static partial IGenericMessage RolePermissionsSetFailed(ILogger logger, string name, string message);

    // List permissions (9173-9175)
    [MessageLogging(EventId = 9173, Level = LogLevel.Trace, Message = "Listing permissions")]
    public static partial IGenericMessage ListingPermissions(ILogger logger);

    [MessageLogging(EventId = 9174, Level = LogLevel.Information, Message = "Listed {count} permissions")]
    public static partial IGenericMessage PermissionsListed(ILogger logger, int count);

    // List permissions grouped (9175-9176)
    [MessageLogging(EventId = 9175, Level = LogLevel.Trace, Message = "Listing permissions grouped by resource")]
    public static partial IGenericMessage ListingPermissionsGrouped(ILogger logger);

    [MessageLogging(EventId = 9176, Level = LogLevel.Information, Message = "Listed {count} permission groups")]
    public static partial IGenericMessage PermissionsGroupedListed(ILogger logger, int count);
}
