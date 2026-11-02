using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceMultitenancy.Sql.Models;

/// <summary>
/// The columns changed when an organization is renamed, disabled or enabled (FDW-1758).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SqlOrganizationUpdate
{
    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the organization is active.</summary>
    public bool IsActive { get; set; }
}
