using System;
using Fdw.Data;

namespace ReferenceMultitenancy.Sql.Models;

/// <summary>
/// Database entity for user-tenant mappings.
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
    /// Gets or sets the user's role within the tenant.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Gets or sets whether this is the user's default tenant.
    /// </summary>
    public bool IsDefault { get; set; }
}
