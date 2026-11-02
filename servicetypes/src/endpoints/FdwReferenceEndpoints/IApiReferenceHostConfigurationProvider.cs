using Fdw.Services.Abstractions;

using Fdw.Services.Hosts.Abstractions;
namespace FdwReferenceEndpoints;

/// <summary>Supplies the ApiReferenceHost implementation's own configuration.</summary>
public interface IApiReferenceHostConfigurationProvider
    : IImplementationConfigurationProvider<IApiReferenceImplementationConfiguration>
{
}
