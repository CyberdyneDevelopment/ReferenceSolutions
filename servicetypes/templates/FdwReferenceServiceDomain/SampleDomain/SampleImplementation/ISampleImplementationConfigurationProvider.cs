using Fdw.Services.Abstractions;
using SampleDomain.Abstractions;

namespace SampleDomain.SampleImplementation;

/// <summary>Supplies the SAMPLE_OPTION implementation's own configuration.</summary>
/// <remarks>
/// Named, rather than consumers naming <c>IImplementationConfigurationProvider</c> closed over the
/// domain contract: every provider registers against, and is depended on through, its own interface.
/// </remarks>
public interface ISampleImplementationConfigurationProvider
    : IImplementationConfigurationProvider<ISampleDomainImplementationConfiguration>
{
}
