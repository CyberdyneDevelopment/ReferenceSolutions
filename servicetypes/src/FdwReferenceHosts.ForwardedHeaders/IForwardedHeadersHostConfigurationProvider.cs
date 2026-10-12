using Fdw.Services.Abstractions;

using Fdw.Services.Hosts.Abstractions;
namespace FdwReferenceHosts.ForwardedHeaders;

/// <summary>Supplies the ForwardedHeadersHost implementation's own configuration.</summary>
public interface IForwardedHeadersHostConfigurationProvider
    : IImplementationConfigurationProvider<IForwardedHeadersImplementationConfiguration>
{
}
