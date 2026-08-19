using System;
using System.Collections.Generic;
using Fdw.Services.Multitenancy.Abstractions;

namespace ReferenceMultitenancy.Sql.Models;

/// <summary>
/// SQL-backed tenant implementation extending TenantTypeBase.
/// A tenant instance. Tenant data is per-tenant runtime state resolved through ITenantProvider,
/// never registered into a process-wide collection.
/// </summary>
public sealed class SqlTenant : TenantTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlTenant"/> class.
    /// </summary>
    /// <param name="id">The tenant unique identifier.</param>
    /// <param name="name">The tenant display name.</param>
    /// <param name="slug">The tenant URL-friendly slug.</param>
    /// <param name="isActive">Whether the tenant is active.</param>
    /// <param name="orgPrefix">The organization prefix applied to permission policy names on the API surface.</param>
    /// <param name="connectionName">The connection name for tenant data.</param>
    /// <param name="theme">The tenant theme configuration.</param>
    /// <param name="options">The tenant options configuration.</param>
    /// <param name="availableRoles">The available roles for this tenant.</param>
    /// <param name="isGlobal">Whether this is the global/home tenant.</param>
    public SqlTenant(
        Guid id,
        string name,
        string slug,
        bool isActive = true,
        string? orgPrefix = null,
        string? connectionName = null,
        ITenantTheme? theme = null,
        ITenantOptions? options = null,
        IEnumerable<string>? availableRoles = null,
        bool isGlobal = false)
        : base(id, name, slug, orgPrefix, connectionName, theme, options, availableRoles, isGlobal)
    {
        SetActive(isActive);
    }

    /// <summary>
    /// Sets the active state of the tenant.
    /// </summary>
    private void SetActive(bool isActive)
    {
        // Use protected setter from base class
        IsActive = isActive;
    }

    /// <summary>
    /// Creates an SqlTenant from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <param name="theme">The tenant theme.</param>
    /// <param name="options">The tenant options.</param>
    /// <param name="roles">The available roles.</param>
    /// <returns>A new SqlTenant instance.</returns>
    public static SqlTenant FromEntity(
        SqlTenantEntity entity,
        ITenantTheme theme,
        ITenantOptions options,
        IEnumerable<string> roles)
    {
        return new SqlTenant(
            id: entity.Id,
            name: entity.DisplayName,
            slug: entity.Slug,
            isActive: entity.IsActive,
            orgPrefix: entity.OrgPrefix,
            connectionName: entity.ConnectionName,
            theme: theme,
            options: options,
            availableRoles: roles,
            isGlobal: entity.IsGlobal);
    }
}
