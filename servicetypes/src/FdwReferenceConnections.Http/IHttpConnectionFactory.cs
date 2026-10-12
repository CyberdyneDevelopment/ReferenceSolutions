using Fdw.Results;
using System.Security.Cryptography.X509Certificates;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Http;


namespace FdwReferenceConnections.Http;

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
    /// <summary>Reports the secrets this connection's security configuration names.</summary>
    /// <param name="configuration">The connection configuration.</param>
    /// <returns>
    /// The secret names to read, and the store to read them from, any of which may be null; or a failure that
    /// names a property the connection's security type requires and the connection does not set.
    /// </returns>
    /// <remarks>
    /// Declared here rather than on <see cref="IConnectionFactory"/> because Http needs THREE
    /// secrets, and a domain-wide contract that named one would be wrong for it. The security
    /// configuration owns which it needs; this only reports them so the provider can fetch them.
    /// </remarks>
    IGenericResult<(string? CertificateSecretName, string? PasswordSecretName, string? ApiKeySecretName, string? SecretManagerName)>
        RequiredSecrets(HttpConnectionConfiguration configuration);

    /// <summary>Creates the connection from already-fetched secrets.</summary>
    /// <param name="configuration">The connection configuration.</param>
    /// <param name="certificate">The client certificate, or null.</param>
    /// <param name="password">The certificate password, or null.</param>
    /// <param name="apiKey">The api key, or null.</param>
    /// <returns>The connection, or a structured failure.</returns>
    /// <remarks>
    /// The certificate stays an X509Certificate2 rather than being flattened to a string, which is
    /// the whole reason this is declared on the implementation's own interface.
    /// </remarks>
    IGenericResult<IGenericConnection> Create(
        HttpConnectionConfiguration configuration,
        X509Certificate2? certificate,
        string? password,
        string? apiKey);

    // Inherited from base interface:
    // IGenericResult<IGenericConnection> Get(HttpConnectionConfiguration configuration);
    // IGenericResult<IGenericConnection> Get(HttpConnectionConfiguration configuration, string connectionType);
}
