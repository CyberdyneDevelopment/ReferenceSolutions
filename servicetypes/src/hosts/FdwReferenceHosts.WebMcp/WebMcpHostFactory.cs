using Fdw.Services;
using Fdw.Configuration;
using Fdw.Results;
using FdwReferenceHosts.WebMcp.Configuration;

namespace FdwReferenceHosts.WebMcp;

/// <summary>Builds the CORS host service from its configuration record.</summary>
public sealed class WebMcpHostFactory : ServiceFactoryBase<WebMcpHostService, WebMcpHostConfiguration>, IWebMcpHostFactory
{

    /// <inheritdoc />
    public override IGenericResult<WebMcpHostService> Create(WebMcpHostConfiguration configuration)
        => Create((IGenericConfiguration)configuration);


}
