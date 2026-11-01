using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuth.Endpoints;

/// <summary>The role to act with (FDW-2282).</summary>
[ExcludeFromCodeCoverage]
public sealed class ActAsRoleRequest
{
    /// <summary>Gets or sets the name of the role to act with.</summary>
    public string Role { get; set; } = string.Empty;
}
