using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuth.Endpoints;

/// <summary>The memberships the calling user holds.</summary>
[ExcludeFromCodeCoverage]
public sealed class MembershipsResponse
{
    /// <summary>Gets or sets the memberships. Never null; may be empty.</summary>
    public IReadOnlyList<MembershipDto> Items { get; set; } = [];
}
