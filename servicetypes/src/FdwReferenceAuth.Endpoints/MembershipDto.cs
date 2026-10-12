using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// One tenant/organisation a user belongs to.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TenantName"/> is nullable and is never filled in from the id. A console has to
/// draw the absence — an id sitting in a name slot is indistinguishable from a real name, and
/// the reader cannot tell they are looking at a fallback.
/// </para>
/// <para>
/// <see cref="Roles"/> is never null and may be empty. Empty means "a member holding nothing",
/// which is a real state a screen renders differently from "we could not find out".
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class MembershipDto
{
    /// <summary>Gets or sets the tenant's identifier.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant's display name, or null when it has none.</summary>
    public string? TenantName { get; set; }

    /// <summary>
    /// Gets or sets the organisation's identifier, or null when the membership is tenant-wide.
    /// Memberships are tenant-wide (FDW-825), so this is null for every item today.
    /// </summary>
    public string? OrgId { get; set; }

    /// <summary>Gets or sets the organisation's name, or null when there is no organisation.</summary>
    public string? OrgName { get; set; }

    /// <summary>Gets or sets whether this is the tenant a sign-in lands in.</summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets whether a tenant picker should offer this tenant. False for the structural
    /// sentinels (the default tenant every user belongs to, the system tenant); a UI leaves those
    /// out. Not an access rule.
    /// </summary>
    public bool IsSelectable { get; set; }

    /// <summary>Gets or sets the roles held in this tenant. Never null; may be empty.</summary>
    public IReadOnlyList<string> Roles { get; set; } = [];
}
