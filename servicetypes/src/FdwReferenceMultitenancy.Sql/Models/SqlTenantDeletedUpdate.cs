using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceMultitenancy.Sql.Models;

/// <summary>
/// The column changed when a tenant is marked deleted or restored (FDW-1759).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SqlTenantDeletedUpdate
{
    /// <summary>Gets or sets whether the tenant is marked deleted.</summary>
    public bool IsDeleted { get; set; }
}
