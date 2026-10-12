using Fdw.Services.Abstractions;

using Fdw.Services.Hosts.Abstractions;
namespace FdwReferenceHosts.Cors;

/// <summary>Supplies the CorsHost implementation's own configuration.</summary>
public interface ICorsHostConfigurationProvider
    : IImplementationConfigurationProvider<ICorsImplementationConfiguration>
{
}
