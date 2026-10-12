using Fdw.Services.Abstractions;

namespace SampleDomain.Abstractions;

/// <summary>
/// Resolves configured SampleDomain members and routes each to the implementation provider that
/// owns it.
/// </summary>
/// <remarks>
/// Reads the domain row from <c>SAMPLE_CATEGORY.SAMPLE_CATEGORY</c> by name or id, takes the
/// <c>Implementation</c> it names, and hands the read to the implementation provider registered under
/// that name — SAMPLE_OPTION to <c>SAMPLE_CATEGORY.SampleImplementation</c>, and any sibling to its
/// own table. What comes back is that implementation's configuration; the domain row is how it is
/// found, never the answer.
/// </remarks>
public interface ISampleDomainConfigurationProvider
    : IDomainConfigurationProvider<ISampleDomainImplementationConfiguration>
{
}
