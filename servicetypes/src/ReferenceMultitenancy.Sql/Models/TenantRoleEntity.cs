using System;
using Fdw.Data;

namespace ReferenceMultitenancy.Sql.Models;

/// <summary>
/// Database entity for tenant available roles.
/// </summary>
[GenerateMapper]
public sealed partial class TenantRoleEntity
{
    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    public string Role { get; set; } = string.Empty;
}
