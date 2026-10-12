using Fdw.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Fdw.Services.Connections.Http.Abstractions.Results;
using Fdw.Services.Connections.Http.Security;
using Fdw.Services.Connections.Http.Security.Types;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.Connections.Http;
using FdwReferenceConnections.Http.Logging;

namespace FdwReferenceConnections.Http;

/// <summary>
/// Factory for creating generic HTTP connection instances.
/// Creates <see cref="HttpConnection"/> instances for HTTP-based communication.
/// </summary>
/// <remarks>
/// <para>
/// This factory accepts <see cref="HttpConnectionConfiguration"/> and creates
/// <see cref="HttpConnection"/> instances. Protocol-specific behavior is determined
/// by the <see cref="IHttpProtocol"/> resolved from the configuration's Protocol name.
/// </para>
/// <para>
/// The factory resolves the protocol from the <see cref="HttpProtocols"/> TypeCollection
/// and builds an <see cref="HttpProtocolContext"/> with resolved secrets for security
/// processing (WS-Security certificates, API keys, etc.).
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage] // Excluded: requires HTTP connections
public sealed class HttpConnectionFactory : ServiceFactoryBase<IGenericConnection, HttpConnectionConfiguration>, IHttpConnectionFactory
{
    private readonly ILogger<HttpConnectionFactory> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    // Why: the factory owns secret resolution and takes the provider by constructor, exactly as it
    // takes IHttpClientFactory — HttpConnectionType registers both in its own Registration phase body.
    // Null only on the provider-less constructor below, which cannot serve a secret-bearing security
    // configuration and says so.
    private readonly ISecretManagerProvider? _secretManagerProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpConnectionFactory"/> class that resolves
    /// secrets through the supplied secret-manager provider.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="httpClientFactory">The HTTP client factory for creating clients.</param>
    /// <param name="loggerFactory">The logger factory for protocol contexts and connection instances.</param>
    /// <param name="secretManagerProvider">The secret-manager provider, resolved by name per connection.</param>
    public HttpConnectionFactory(
        ILogger<HttpConnectionFactory> logger,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        ISecretManagerProvider secretManagerProvider)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _secretManagerProvider = secretManagerProvider;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpConnectionFactory"/> class with no
    /// secret-manager provider — usable only for connections whose security needs no secret.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="httpClientFactory">The HTTP client factory for creating clients.</param>
    /// <param name="loggerFactory">The logger factory for protocol contexts and connection instances.</param>
    public HttpConnectionFactory(
        ILogger<HttpConnectionFactory> logger,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _secretManagerProvider = null;
    }



    /// <summary>
    /// Creates an HTTP connection from its <see cref="HttpConnectionConfiguration"/> implementation record.
    /// </summary>
    /// <param name="configuration">The HTTP connection configuration.</param>
    /// <returns>A result containing the created connection or failure information.</returns>
    /// <remarks>
    /// The connection name is the record's own <c>Name</c>, stamped by the domain provider when it
    /// read the record. Nothing is resolved on this path: a configuration that declares secrets
    /// needs the overload that takes them.
    /// </remarks>
    public override IGenericResult<IGenericConnection> Create(HttpConnectionConfiguration configuration)
        => Create(configuration, certificate: null, password: null, apiKey: null);

    /// <inheritdoc />
    public IGenericResult<(string? CertificateSecretName, string? PasswordSecretName, string? ApiKeySecretName, string? SecretManagerName)>
        RequiredSecrets(HttpConnectionConfiguration configuration)
    {
        var requirement = ResolveSecurityRequirement(configuration);
        if (!requirement.IsSuccess)
            return requirement.ToNewResult<(string?, string?, string?, string?)>();

        var (_, certificateSecretName, passwordSecretName, apiKeySecretName) = requirement.Value;
        return GenericResult<(string?, string?, string?, string?)>.Success(
            (certificateSecretName, passwordSecretName, apiKeySecretName, GetSecretManagerName(configuration)));
    }

    /// <inheritdoc />
    public IGenericResult<IGenericConnection> Create(
        HttpConnectionConfiguration configuration,
        X509Certificate2? certificate,
        string? password,
        string? apiKey)
    {
        if (configuration is null)
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.ConfigurationNull(_logger));

        var connectionName = configuration.Name;
        var effectiveName = connectionName.Length > 0 ? connectionName : configuration.Id.ToString();
        HttpConnectionFactoryLogger.TraceCreateEntry(_logger, effectiveName);
        HttpConnectionFactoryLogger.CreatingConnection(_logger, connectionName);

        HttpConnectionFactoryLogger.TraceResolvingProtocol(_logger, configuration.Protocol, connectionName);
        var protocol = ResolveProtocol(configuration, connectionName);
        if (protocol is null)
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.ProtocolNotFound(_logger, connectionName, configuration.Protocol));

        return AssembleConnection(configuration, connectionName, protocol, certificate, password, apiKey);
    }





    #region Helper Methods

    /// <summary>
    /// Resolves the protocol from the HttpProtocols TypeCollection.
    /// </summary>
    private IHttpProtocol? ResolveProtocol(HttpConnectionConfiguration configuration, string connectionName)
    {
        var protocolName = configuration.Protocol;
        if (string.IsNullOrWhiteSpace(protocolName))
        {
            HttpConnectionFactoryLogger.NoProtocolSpecified(_logger, connectionName);
            // Default to REST if no protocol specified
            protocolName = "Rest";
        }

        var protocol = HttpProtocols.ByName(protocolName);
        if (protocol == null || string.Equals(protocol.Name, "NotFound", StringComparison.Ordinal))
        {
            return null;
        }

        return protocol;
    }

    /// <summary>
    /// Determines whether this connection's bound security configuration demands a secret, and if so,
    /// which secret names it needs.
    /// </summary>
    /// <remarks>
    /// Why: binding the security TypeOption instance is the only way to know — each concrete security
    /// type (WsSecurity, UsernameToken, ApiKey, Basic) owns its own secret-name property via
    /// <see cref="GetSecretNames"/>. The factory never inspects the raw
    /// <see cref="HttpConnectionConfigurationBase.AdditionalProperties"/> KVP dictionary directly. No bound
    /// security (empty dict or unresolved/None AuthenticationType) means no secret is required.
    /// </remarks>
    // Why: the secret-manager NAME lives in the authentication KVP — a key the secret-backed
    // HttpAuthenticationTypes option declares — never a column on the connection header.
    private static string? GetSecretManagerName(HttpConnectionConfiguration configuration)
        => configuration.AdditionalProperties.TryGetValue("SecretManagerName", out var name) ? name : null;

    private IGenericResult<(HttpAuthenticationConfiguration? Instance, string? CertificateSecretName, string? PasswordSecretName, string? ApiKeySecretName)>
        ResolveSecurityRequirement(HttpConnectionConfiguration configuration)
    {
        // Why an AuthenticationType no security type answers to is refused rather than read as no security: a
        // misspelling (Bearr) would otherwise build a connection that sends no credential at all. The column is
        // NOT NULL DEFAULT 'None', so a connection that wants none says so.
        var securityType = HttpAuthenticationTypes.ByName(configuration.AuthenticationType);
        if (securityType == null || string.IsNullOrEmpty(securityType.Name))
            return GenericResult<(HttpAuthenticationConfiguration?, string?, string?, string?)>.Failure(
                HttpResultCodes.ByName("UnknownSecurityType"),
                ResultDetails.Create().With("SecurityType", configuration.AuthenticationType));

        // Cheap fast path: RequiresSecret is a constructor-set fact about the security TYPE, so a
        // type that never carries a secret (None) skips binding a values-dependent instance.
        if (!securityType.RequiresSecret)
            return GenericResult<(HttpAuthenticationConfiguration?, string?, string?, string?)>.Success((null, null, null, null));

        // Why every property the type requires is checked before binding: one left unset (missing, or misspelled
        // as TokenSecret) binds to a null secret name, the provider then reads no secret, and the connection goes
        // out with no credential at all, so a 401 from the server would be the first sign. Each security type
        // declares its own RequiredProperties, and this is where a connection is checked against them.
        var values = new Dictionary<string, string?>(configuration.AdditionalProperties, StringComparer.OrdinalIgnoreCase);
        foreach (var property in securityType.RequiredProperties)
        {
            if (!values.TryGetValue(property, out var value) || string.IsNullOrWhiteSpace(value))
                return GenericResult<(HttpAuthenticationConfiguration?, string?, string?, string?)>.Failure(
                    HttpConnectionFactoryLogger.SecurityPropertyMissing(_logger, configuration.Name, securityType.Name, property));
        }

        var securityInstance = securityType.CreateInstance();
        securityInstance.BindFromValues(values);

        var (certificateSecretName, passwordSecretName, apiKeySecretName) = GetSecretNames(securityInstance);
        return GenericResult<(HttpAuthenticationConfiguration?, string?, string?, string?)>.Success(
            (securityInstance, certificateSecretName, passwordSecretName, apiKeySecretName));
    }

    /// <summary>Maps a bound security configuration to the secret names it demands.</summary>
    private static (string? CertificateSecretName, string? PasswordSecretName, string? ApiKeySecretName) GetSecretNames(
        HttpAuthenticationConfiguration security)
        => security switch
        {
            WsSecurityConfiguration ws => (ws.CertificateSecretName, null, null),
            UsernameTokenConfiguration ut => (null, ut.PasswordSecretName, null),
            ApiKeySecurityConfiguration ak => (null, null, ak.ApiKeySecretName),
            // A bearer token is a key credential: it travels in the API-key slot, and the protocol sends
            // it as Authorization: Bearer because the connection's AuthenticationType is Bearer.
            BearerSecurityConfiguration bearer => (null, null, bearer.TokenSecretName),
            BasicSecurityConfiguration basic => (null, basic.PasswordSecretName, null),
            _ => (null, null, null),
        };





    /// <summary>
    /// Assembles the protocol context, HTTP client, and connection instance from already-resolved
    /// secrets. Shared tail of both the sync (no-secret) and async (secret-resolving) build paths.
    /// </summary>
    private IGenericResult<IGenericConnection> AssembleConnection(
        HttpConnectionConfiguration configuration,
        string connectionName,
        IHttpProtocol protocol,
        X509Certificate2? certificate,
        string? password,
        string? apiKey)
    {
        var context = new HttpProtocolContext(configuration, _loggerFactory, certificate, password, apiKey);

        var httpClient = configuration.UseMtls && certificate is not null
            ? CreateMtlsHttpClient(configuration, protocol, certificate)
            : CreateHttpClient(configuration, connectionName, protocol);

        if (configuration.UseMtls && certificate is not null)
        {
            HttpConnectionFactoryLogger.MtlsClientCreated(_logger, connectionName);
        }
        else if (configuration.UseMtls)
        {
            HttpConnectionFactoryLogger.MtlsCertificateRequiredButMissing(_logger, connectionName);
        }

        var connection = new HttpConnection(
            _loggerFactory.CreateLogger<HttpConnection>(),
            configuration,
            httpClient,
            protocol,
            context);

        HttpConnectionFactoryLogger.ConnectionCreated(_logger, connectionName, configuration.BaseUrl);

        return GenericResult<IGenericConnection>.Success(connection);
    }

    /// <summary>
    /// Creates an HTTP client configured for the specified connection and protocol.
    /// </summary>
    private HttpClient CreateHttpClient(
        HttpConnectionConfiguration configuration,
        string connectionName,
        IHttpClientFactory httpClientFactory,
        IHttpProtocol protocol)
    {
        // Why: IHttpClientFactory.CreateClient uses the name as the named-options key. The name is the
        // record's stamped Name; a record read outside a domain provider carries none, so its
        // ConnectionId keys the client instead.
        var clientName = connectionName.Length > 0 ? connectionName : configuration.Id.ToString();
        var client = httpClientFactory.CreateClient(clientName);

        // Configure client from configuration
        client.BaseAddress = BuildBaseAddress(configuration.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(configuration.TimeoutSeconds);

        // Add default headers - use protocol's content type if not specified in config
        client.DefaultRequestHeaders.Clear();
        var contentType = configuration.ContentType ?? protocol.DefaultContentType;
        client.DefaultRequestHeaders.Add("Accept", contentType);
        client.DefaultRequestHeaders.Add("User-Agent", "Fdw-HTTP-Client/1.0");
        ApplyConfiguredHeaders(client, configuration);

        HttpConnectionFactoryLogger.ConfiguringHttpClient(_logger, connectionName, configuration.BaseUrl, configuration.TimeoutSeconds);

        return client;
    }

    // Why configured headers are applied LAST, replacing rather than adding: the two set above are what
    // the framework can guess, and a connection that names the same header means to override it. Add
    // would keep both values on the wire, which for User-Agent is how a request gets rejected by a host
    // that inspects it. A null value removes the header outright, so a connection can also say "send no
    // Accept" — that is a real requirement for some APIs and there is no other way to express it.
    private static void ApplyConfiguredHeaders(HttpClient client, HttpConnectionConfiguration configuration)
    {
        if (configuration.Headers.Count == 0)
            return;

        foreach (var header in configuration.Headers)
        {
            if (string.IsNullOrWhiteSpace(header.Key))
                continue;

            client.DefaultRequestHeaders.Remove(header.Key);
            if (header.Value is not null)
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }

    // Why the trailing slash is forced: HttpClient resolves a relative request path against
    // BaseAddress by RFC 3986, which REPLACES the base's last segment when the base does not end in
    // '/'. A base of ".../api/v1" with the path "teams" therefore requests ".../api/teams" — the
    // version segment silently disappears and the host answers 404 for a URL that is correct
    // everywhere it is written down. A DataPath is appended to a base, never substituted into it,
    // so the base is normalized to say that.
    private static Uri BuildBaseAddress(string baseUrl)
        => new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");

    /// <summary>
    /// Creates an HTTP client configured for HTTP requests using the injected factory.
    /// </summary>
    private HttpClient CreateHttpClient(HttpConnectionConfiguration configuration, string connectionName, IHttpProtocol protocol)
        => CreateHttpClient(configuration, connectionName, _httpClientFactory, protocol);

    /// <summary>
    /// Creates an HTTP client with a client certificate attached for mutual TLS.
    /// </summary>
    private static HttpClient CreateMtlsHttpClient(
        HttpConnectionConfiguration configuration,
        IHttpProtocol protocol,
        X509Certificate2 clientCertificate)
    {
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(clientCertificate);
        var client = new HttpClient(handler);
        client.BaseAddress = BuildBaseAddress(configuration.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(configuration.TimeoutSeconds);
        client.DefaultRequestHeaders.Clear();
        var contentType = configuration.ContentType ?? protocol.DefaultContentType;
        client.DefaultRequestHeaders.Add("Accept", contentType);
        client.DefaultRequestHeaders.Add("User-Agent", "Fdw-HTTP-Client/1.0");
        ApplyConfiguredHeaders(client, configuration);
        return client;
    }

    #endregion

}
