using System.Diagnostics.CodeAnalysis;

namespace SampleDomain.Clients.Models;

/// <summary>One SampleDomain resource as the API returns it.</summary>
[ExcludeFromCodeCoverage]
public class SampleDomainPayload
{
    /// <summary>Gets or sets the name this resource is addressed by.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a human-readable description.</summary>
    public string? Description { get; set; }
}
