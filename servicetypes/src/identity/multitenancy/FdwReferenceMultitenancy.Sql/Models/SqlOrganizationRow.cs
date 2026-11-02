using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace FdwReferenceMultitenancy.Sql.Models;

/// <summary>
/// The key of an organization row, which its implementation row points at (FDW-1758).
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class SqlOrganizationRow
{
    /// <summary>Gets or sets the row's key.</summary>
    public int RowId { get; set; }

    /// <summary>Gets or sets the organization's identifier.</summary>
    public Guid Id { get; set; }
}
