using Fdw.Services;
using Fdw.Configuration;
using Fdw.Results;
using FdwReferenceHosts.Cors.Configuration;

namespace FdwReferenceHosts.Cors;

/// <summary>Builds the CORS host service from its configuration record.</summary>
public sealed class CorsHostFactory : ServiceFactoryBase<CorsHostService, CorsHostConfiguration>, ICorsHostFactory
{

    /// <inheritdoc />
    public override IGenericResult<CorsHostService> Create(CorsHostConfiguration configuration)
        => Create((IGenericConfiguration)configuration);


}
