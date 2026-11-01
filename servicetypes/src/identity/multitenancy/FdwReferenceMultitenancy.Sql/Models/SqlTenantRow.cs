using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace FdwReferenceMultitenancy.Sql.Models;

/// <summary>
/// The key of a tenant row, which an organization row points at (FDW-1758).
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class SqlTenantRow
{
    /// <summary>Gets or sets the row's key.</summary>
    public int RowId { get; set; }

    /// <summary>Gets or sets the tenant's identifier.</summary>
    public Guid Id { get; set; }
}
