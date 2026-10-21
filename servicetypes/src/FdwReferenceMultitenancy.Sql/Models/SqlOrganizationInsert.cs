using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace FdwReferenceMultitenancy.Sql.Models;

/// <summary>
/// The columns written when an organization is made (FDW-1758): its domain row in <c>tenant.Organization</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class SqlOrganizationInsert
{
    /// <summary>Gets or sets the organization's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the configuration domain the row belongs to.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the implementation that holds the row's details.</summary>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant's identifier.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the key of the tenant's row.</summary>
    public int TenantRowId { get; set; }

    /// <summary>Gets or sets the slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this is the tenant's default organization.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Gets or sets whether this is the platform's administration organization.</summary>
    public bool IsAdminOrg { get; set; }

    /// <summary>Gets or sets whether the organization is active.</summary>
    public bool IsActive { get; set; }
}
