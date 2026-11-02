using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace FdwReferenceMultitenancy.Sql.Models;

/// <summary>
/// The columns a new tenant row states (FDW-1578). Every other column of <c>tenant.Tenants</c> has its own default, and the theme,
/// limits and features are set afterwards by whoever administers the tenant.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class SqlTenantInsert
{
    /// <summary>Gets or sets the tenant's durable id, minted by the writer.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the tenant's slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the permission policy prefix; null for none.</summary>
    public string? OrgPrefix { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the tenant is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets a value indicating whether the tenant is the global tenant; never true for a tenant made here.</summary>
    public bool IsGlobal { get; set; }

    /// <summary>Gets or sets a value indicating whether a person can list and switch into the tenant.</summary>
    public bool IsSelectable { get; set; }

    /// <summary>Gets or sets the connection name that isolates the tenant's data; null for none.</summary>
    public string? ConnectionName { get; set; }
}
