using System;
using Fdw.Configuration;

namespace SampleDomain.Abstractions;

/// <summary>
/// The contract every SampleDomain implementation configuration satisfies — SAMPLE_OPTION's, and
/// every sibling implementation added alongside it.
/// </summary>
/// <remarks>
/// <para>
/// A configured SampleDomain member is two rows. The domain row is the platform's one
/// <see cref="DomainConfiguration"/> record, in <c>SAMPLE_CATEGORY.SAMPLE_CATEGORY</c>: its Id, its
/// Name, its Domain and the Implementation that selects it, and nothing else. The implementation row
/// lives in that implementation's own table and carries every field the implementation reads at
/// runtime.
/// </para>
/// <para>
/// Name, Domain and Implementation are the domain row's. The domain provider stamps them onto the
/// implementation on every read, and they are never persisted on the implementation row.
/// </para>
/// </remarks>
public interface ISampleDomainImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the Id of the domain row this implementation belongs to.</summary>
    Guid SampleDomainId { get; set; }
}
