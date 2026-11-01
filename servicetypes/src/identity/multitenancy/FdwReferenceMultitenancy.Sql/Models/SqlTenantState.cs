using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace FdwReferenceMultitenancy.Sql.Models;

/// <summary>
/// What is read of a tenant row to decide whether it may be marked deleted or restored (FDW-1759), deleted rows included.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class SqlTenantState
{
    /// <summary>Gets or sets the tenant's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets whether the tenant is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets whether this is the global tenant.</summary>
    public bool IsGlobal { get; set; }

    /// <summary>Gets or sets whether the tenant is marked deleted.</summary>
    public bool IsDeleted { get; set; }
}
