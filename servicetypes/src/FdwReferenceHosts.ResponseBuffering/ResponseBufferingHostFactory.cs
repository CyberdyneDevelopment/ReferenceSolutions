using Fdw.Services;
using Fdw.Configuration;
using Fdw.Results;
using FdwReferenceHosts.ResponseBuffering.Configuration;

namespace FdwReferenceHosts.ResponseBuffering;

/// <summary>Builds the response-buffering host service from its configuration record.</summary>
public sealed class ResponseBufferingHostFactory : ServiceFactoryBase<ResponseBufferingHostService, ResponseBufferingHostConfiguration>, IResponseBufferingHostFactory
{

    /// <inheritdoc />
    public override IGenericResult<ResponseBufferingHostService> Create(ResponseBufferingHostConfiguration configuration)
        => Create((IGenericConfiguration)configuration);


}
