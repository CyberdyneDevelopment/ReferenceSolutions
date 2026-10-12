using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using SampleDomain.Abstractions;

namespace SampleDomain.SampleImplementation;

/// <summary>
/// Configuration for the SAMPLE_OPTION implementation of SampleDomain.
/// </summary>
/// <remarks>
/// Persisted to <c>SAMPLE_CATEGORY.SampleImplementation</c> and joined to its domain row through
/// <see cref="SampleDomainId"/>. Every field this implementation's factory reads at runtime lives
/// here — the domain row carries only identity and the Implementation that selects this type, so a
/// field put there would read as empty after dispatch.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "SAMPLE_CATEGORY", ServiceType = "SAMPLE_OPTION")]
public sealed partial class SampleImplementationConfiguration : ISampleDomainImplementationConfiguration
{
    /// <summary>Gets or sets this implementation row's own identifier.</summary>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    public Guid SampleDomainId { get; set; }

    /// <summary>
    /// An example setting. Replace with whatever this implementation's factory actually needs — a
    /// server, an endpoint, a timeout, whatever makes this option a distinct choice from its siblings.
    /// </summary>
    /// <remarks>Unset until configured: a missing value fails where it is read, not as a plausible default here.</remarks>
    public string? Example { get; set; }

    /// <inheritdoc/>
    /// <remarks>Set by the domain provider from the domain row; never persisted.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the domain provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the domain provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;
}
