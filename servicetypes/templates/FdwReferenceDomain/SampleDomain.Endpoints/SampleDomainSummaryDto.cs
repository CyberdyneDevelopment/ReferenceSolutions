using System.Diagnostics.CodeAnalysis;

namespace SampleDomain.Endpoints;

/// <summary>What the list endpoint returns for one SampleDomain resource.</summary>
[ExcludeFromCodeCoverage]
public class SampleDomainSummaryDto
{
    /// <summary>Gets or sets the name this resource is addressed by.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a human-readable description.</summary>
    public string? Description { get; set; }
}
