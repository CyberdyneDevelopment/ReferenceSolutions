using Fdw.Services.Abstractions;

using Fdw.Services.Hosts.Abstractions;
namespace FdwReferenceHosts.ResponseBuffering;

/// <summary>Supplies the ResponseBufferingHost implementation's own configuration.</summary>
public interface IResponseBufferingHostConfigurationProvider
    : IImplementationConfigurationProvider<IResponseBufferingImplementationConfiguration>
{
}
