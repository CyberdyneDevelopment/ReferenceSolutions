using Fdw.Configuration;
using Fdw.Services.Connections.Abstractions;

using Fdw.Services.Connections;
using Fdw.Services.Connections.Http;

using Fdw.Services.Connections.Http.Limits;

using Fdw.Services.Connections.Http.Results;

using Fdw.Services.Connections.Http.Logging;

using Fdw.Services.Connections.Http.Commands;

using Fdw.Services.Connections.Http.Validation;

using Fdw.Services.Connections.Http.Protocols;

using Fdw.Services.Connections.Http.Security;


namespace ReferenceConnections.Http;

/// <summary>
/// Factory interface for creating generic HTTP connection instances.
/// Creates <see cref="HttpConnection"/> instances configured for HTTP-based communication.
/// </summary>
/// <remarks>
/// <para>
/// This interface inherits from <see cref="IConnectionFactory{TConnection, TConfiguration}"/>
/// with <see cref="IGenericConnection"/> as the connection type and
/// <see cref="HttpConnectionConfiguration"/> as the configuration type.
/// </para>
/// <para>
/// The base interface provides:
/// - Get(HttpConnectionConfiguration)
/// - Get(IGenericConfiguration)
/// - Get methods from IServiceFactory hierarchy
/// </para>
/// <para>
/// For protocol-specific factories (REST, OData, GraphQL, SOAP), use the appropriate
/// specialized factory interfaces (IRestConnectionFactory, IODataConnectionFactory, etc.).
/// </para>
/// </remarks>
public interface IHttpConnectionFactory : IConnectionFactory<IGenericConnection, HttpConnectionConfiguration>
{
    // Inherited from base interface:
    // IGenericResult<IGenericConnection> Get(HttpConnectionConfiguration configuration);
    // IGenericResult<IGenericConnection> Get(HttpConnectionConfiguration configuration, string connectionType);
}
