using Fdw.Services.Hosts.Abstractions;
using FdwReferenceHosts.SecurityHeaders.Configuration;

namespace FdwReferenceHosts.SecurityHeaders;

/// <summary>Builds the CORS host service from its configuration.</summary>
public interface ISecurityHeadersHostFactory : IHostFactory<SecurityHeadersHostService, SecurityHeadersHostConfiguration>
{
}
