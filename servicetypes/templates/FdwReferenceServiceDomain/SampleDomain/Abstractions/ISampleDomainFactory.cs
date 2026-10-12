using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Services.Abstractions;

namespace SampleDomain.Abstractions;

/// <summary>
/// Marks a factory that produces this domain's service.
/// </summary>
/// <remarks>
/// The non-generic form is what a collection and its registrations name when the service and its
/// configuration are not yet known; the generic form below is what an option's factory implements.
/// </remarks>
public interface ISampleDomainFactory
{
}

/// <summary>
/// Produces <typeparamref name="TService"/> from <typeparamref name="TConfiguration"/>.
/// </summary>
/// <typeparam name="TService">The service this factory builds.</typeparam>
/// <typeparam name="TConfiguration">The configuration it builds from.</typeparam>
public interface ISampleDomainFactory<TService, TConfiguration>
    : ISampleDomainFactory, IServiceFactory<TService, TConfiguration>
    where TService : IGenericService
    where TConfiguration : IGenericConfiguration
{
}
