using Fdw.Services.Abstractions;

using Fdw.Services.Hosts.Abstractions;
namespace FdwReferenceHosts.EmptyBody;

/// <summary>Supplies the EmptyBodyHost implementation's own configuration.</summary>
public interface IEmptyBodyHostConfigurationProvider
    : IImplementationConfigurationProvider<IEmptyBodyImplementationConfiguration>
{
}
