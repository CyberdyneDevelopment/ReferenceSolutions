using Fdw.Services.Hosts.Abstractions;
using FdwReferenceHosts.ResponseStreaming.Configuration;

namespace FdwReferenceHosts.ResponseStreaming;

/// <summary>Builds the response-streaming host service from its configuration.</summary>
public interface IResponseStreamingHostFactory : IHostFactory<ResponseStreamingHostService, ResponseStreamingHostConfiguration>
{
}
