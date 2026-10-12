using Fdw.Services.Abstractions;

using Fdw.Services.Connections.Abstractions;
namespace FdwReferenceConnections.Http;

/// <summary>Supplies the HttpConnection implementation's own configuration.</summary>
public interface IHttpConnectionConfigurationProvider
    : IImplementationConfigurationProvider<IConnectionImplementationConfiguration>
{
}
