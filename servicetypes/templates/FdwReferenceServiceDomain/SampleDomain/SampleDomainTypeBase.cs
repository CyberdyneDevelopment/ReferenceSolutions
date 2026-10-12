using Fdw.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.Data.Abstractions;
using SampleDomain.Abstractions;

namespace SampleDomain;

/// <summary>
/// Base class every SampleDomain implementation option closes. Every implementation added
/// alongside <see cref="SampleImplementation.SampleImplementationType"/> derives from this.
/// </summary>
/// <typeparam name="TService">The runtime service an implementation constructs. Use
/// <c>IGenericService</c> until this domain has a real runtime service of its own — see the README for
/// what to add when it does.</typeparam>
/// <typeparam name="TFactory">The factory type for the implementation.</typeparam>
/// <typeparam name="TConfiguration">The implementation's own configuration (e.g.
/// <see cref="SampleImplementation.SampleImplementationConfiguration"/>). The domain row is the shared
/// <c>DomainConfiguration</c>, so there is no domain type to close this on instead.</typeparam>
public abstract class SampleDomainTypeBase<TService, TFactory, TConfiguration>
    : ServiceTypeBase<TService, TFactory, TConfiguration>, ISampleDomainType
    where TService : IGenericService
    where TConfiguration : class, ISampleDomainImplementationConfiguration
    where TFactory : ISampleDomainFactory<TService, TConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SampleDomainTypeBase{TService, TFactory, TConfiguration}"/> class.</summary>
    /// <param name="name">The option's discriminator within <see cref="SampleDomainTypes"/> — its <c>Implementation</c>.</param>
    /// <param name="sectionName">The configuration section this option reads.</param>
    /// <param name="displayName">Human-readable name.</param>
    /// <param name="description">What this implementation is for.</param>
    /// <param name="defaultContainerName">The table this implementation's rows live in.</param>
    protected SampleDomainTypeBase(string name, string sectionName, string displayName, string description, string defaultContainerName)
        : base(name, sectionName, displayName, description,
               category: "SAMPLE_CATEGORY",
               defaultDataStoreName: () => PlatformConnection.Name,
               defaultPathName: "SAMPLE_CATEGORY",
               defaultContainerName: defaultContainerName)
    {
    }
}
