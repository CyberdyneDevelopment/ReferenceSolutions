using Fdw.Services;
using Fdw.Configuration;
using Fdw.Results;
using FdwReferenceHosts.OutboundIdentity.Configuration;

namespace FdwReferenceHosts.OutboundIdentity;

/// <summary>Builds the outbound-identity host service from its configuration record.</summary>
public sealed class OutboundIdentityHostFactory : ServiceFactoryBase<OutboundIdentityHostService, OutboundIdentityHostConfiguration>, IOutboundIdentityHostFactory
{

    /// <inheritdoc />
    public override IGenericResult<OutboundIdentityHostService> Create(OutboundIdentityHostConfiguration configuration)
        => Create((IGenericConfiguration)configuration);


}
