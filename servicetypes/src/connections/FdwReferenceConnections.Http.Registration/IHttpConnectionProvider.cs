using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;

namespace FdwReferenceConnections.Http;

/// <summary>The HttpConnection implementation of its domain.</summary>
public interface IHttpConnectionProvider : IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>
{
}
