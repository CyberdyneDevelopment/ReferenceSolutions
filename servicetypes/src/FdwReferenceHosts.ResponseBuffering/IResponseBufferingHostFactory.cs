using Fdw.Services.Hosts.Abstractions;
using FdwReferenceHosts.ResponseBuffering.Configuration;

namespace FdwReferenceHosts.ResponseBuffering;

/// <summary>Builds the response-buffering host service from its configuration.</summary>
public interface IResponseBufferingHostFactory : IHostFactory<ResponseBufferingHostService, ResponseBufferingHostConfiguration>
{
}
