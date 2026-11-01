using Fdw.Services.Abstractions;
using Fdw.Services.Hosts.Abstractions;

namespace FdwReferenceHosts.WebMcp;

/// <summary>Supplies the WebMcpHost implementation's own configuration.</summary>
public interface IWebMcpHostConfigurationProvider
    : IImplementationConfigurationProvider<IWebMcpImplementationConfiguration>
{
}
