using Fdw.Services.Abstractions;

using Fdw.Services.Hosts.Abstractions;
namespace FdwReferenceHosts.OutboundIdentity;

/// <summary>Supplies the OutboundIdentityHost implementation's own configuration.</summary>
public interface IOutboundIdentityHostConfigurationProvider
    : IImplementationConfigurationProvider<IOutboundIdentityImplementationConfiguration>
{
}
