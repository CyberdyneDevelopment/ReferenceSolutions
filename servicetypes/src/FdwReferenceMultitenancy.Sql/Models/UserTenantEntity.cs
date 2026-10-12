using System;
using Fdw.Data;

namespace FdwReferenceMultitenancy.Sql.Models;

/// <summary>
/// A tenant membership — a row of the membership IMPLEMENTATION table
/// (<c>tenant.UserTenantsConfiguration</c>), which is where the user and tenant live. The domain
/// table (<c>tenant.UserTenants</c>) carries only the member's name.
/// </summary>
[GenerateMapper]
public sealed partial class UserTenantEntity
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets whether the membership is the current version.
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Gets or sets whether the membership was removed.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets whether this is the user's default tenant.
    /// </summary>
    public bool IsDefault { get; set; }
}
