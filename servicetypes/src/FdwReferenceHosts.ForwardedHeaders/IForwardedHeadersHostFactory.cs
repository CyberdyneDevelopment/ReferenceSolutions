using Fdw.Services.Hosts.Abstractions;
using FdwReferenceHosts.ForwardedHeaders.Configuration;

namespace FdwReferenceHosts.ForwardedHeaders;

/// <summary>Builds the CORS host service from its configuration.</summary>
public interface IForwardedHeadersHostFactory : IHostFactory<ForwardedHeadersHostService, ForwardedHeadersHostConfiguration>
{
}
