using Fdw.Services;
using Fdw.Configuration;
using Fdw.Results;
using FdwReferenceHosts.ForwardedHeaders.Configuration;

namespace FdwReferenceHosts.ForwardedHeaders;

/// <summary>Builds the CORS host service from its configuration record.</summary>
public sealed class ForwardedHeadersHostFactory : ServiceFactoryBase<ForwardedHeadersHostService, ForwardedHeadersHostConfiguration>, IForwardedHeadersHostFactory
{

    /// <inheritdoc />
    public override IGenericResult<ForwardedHeadersHostService> Create(ForwardedHeadersHostConfiguration configuration)
        => Create((IGenericConfiguration)configuration);


}
