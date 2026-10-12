using Fdw.Services.Abstractions;

using Fdw.Services.Hosts.Abstractions;
namespace FdwReferenceHosts.ResponseStreaming;

/// <summary>Supplies the ResponseStreamingHost implementation's own configuration.</summary>
public interface IResponseStreamingHostConfigurationProvider
    : IImplementationConfigurationProvider<IResponseStreamingImplementationConfiguration>
{
}
