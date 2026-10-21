using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceMultitenancy.Sql.Models;

/// <summary>
/// The columns a change to a tenant writes (FDW-1578): the three a change can state. The writer fills in what the change leaves out
/// from the tenant as it is, so one write carries the whole of them and an unstated part keeps its value.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SqlTenantUpdate
{
    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the tenant is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the connection name; null for none.</summary>
    public string? ConnectionName { get; set; }
}
