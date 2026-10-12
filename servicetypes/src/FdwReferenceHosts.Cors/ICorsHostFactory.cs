using Fdw.Services.Hosts.Abstractions;
using FdwReferenceHosts.Cors.Configuration;

namespace FdwReferenceHosts.Cors;

/// <summary>Builds the CORS host service from its configuration.</summary>
public interface ICorsHostFactory : IHostFactory<CorsHostService, CorsHostConfiguration>
{
}
