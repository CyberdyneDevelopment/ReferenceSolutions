using Fdw.Services.Hosts.Abstractions;
using FdwReferenceHosts.OutboundIdentity.Configuration;

namespace FdwReferenceHosts.OutboundIdentity;

/// <summary>Builds the outbound-identity host service from its configuration.</summary>
public interface IOutboundIdentityHostFactory : IHostFactory<OutboundIdentityHostService, OutboundIdentityHostConfiguration>
{
}
