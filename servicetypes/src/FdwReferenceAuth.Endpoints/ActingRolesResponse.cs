using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuth.Endpoints;

/// <summary>The roles a session may act with (FDW-2282).</summary>
[ExcludeFromCodeCoverage]
public sealed class ActingRolesResponse
{
    /// <summary>Gets or sets the role names.</summary>
    public IReadOnlyList<string> Roles { get; set; } = [];
}
