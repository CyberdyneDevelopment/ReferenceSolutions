using Fdw.Services;
using Fdw.Configuration;
using Fdw.Results;
using FdwReferenceHosts.ResponseStreaming.Configuration;

namespace FdwReferenceHosts.ResponseStreaming;

/// <summary>Builds the response-streaming host service from its configuration record.</summary>
public sealed class ResponseStreamingHostFactory : ServiceFactoryBase<ResponseStreamingHostService, ResponseStreamingHostConfiguration>, IResponseStreamingHostFactory
{

    /// <inheritdoc />
    public override IGenericResult<ResponseStreamingHostService> Create(ResponseStreamingHostConfiguration configuration)
        => Create((IGenericConfiguration)configuration);


}
