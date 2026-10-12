using Fdw.Services;
using Fdw.Configuration;
using Fdw.Results;
using FdwReferenceHosts.EmptyBody.Configuration;

namespace FdwReferenceHosts.EmptyBody;

/// <summary>Builds the CORS host service from its configuration record.</summary>
public sealed class EmptyBodyHostFactory : ServiceFactoryBase<EmptyBodyHostService, EmptyBodyHostConfiguration>, IEmptyBodyHostFactory
{

    /// <inheritdoc />
    public override IGenericResult<EmptyBodyHostService> Create(EmptyBodyHostConfiguration configuration)
        => Create((IGenericConfiguration)configuration);


}
