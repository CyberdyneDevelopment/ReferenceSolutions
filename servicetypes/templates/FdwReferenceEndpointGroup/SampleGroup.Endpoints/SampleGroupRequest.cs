using System;

namespace SampleGroup.Endpoints;

/// <summary>
/// What a SampleGroup operation accepts.
/// </summary>
/// <remarks>
/// Every property is nullable on purpose for a modify operation: "not supplied" and "set to empty"
/// have to stay distinguishable, or a partial update silently blanks the fields the caller never
/// mentioned. Keep that property when you add fields.
/// </remarks>
public sealed class SampleGroupRequest
{
    /// <summary>Gets or sets the item's identifier, bound from the route.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the item's name.</summary>
    public string? Name { get; set; }
}
