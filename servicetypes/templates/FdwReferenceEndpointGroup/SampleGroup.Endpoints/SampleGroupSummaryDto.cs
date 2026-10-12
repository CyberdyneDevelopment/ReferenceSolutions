using System;

namespace SampleGroup.Endpoints;

/// <summary>
/// What a SampleGroup item looks like on the wire.
/// </summary>
/// <remarks>
/// A separate type from whatever the service returns, deliberately. That boundary is what lets the
/// service change shape without breaking the wire contract, and it is why this lives in the
/// endpoint package rather than being borrowed from a domain package.
/// </remarks>
public sealed class SampleGroupSummaryDto
{
    /// <summary>Gets or sets the item's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the item's name.</summary>
    public string Name { get; set; } = string.Empty;
}
