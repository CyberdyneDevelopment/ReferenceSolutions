using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Fdw.Services.Connections.Http.Abstractions.Results;
using Fdw.Services.Connections.Http.Logging;
using Fdw.Services.Connections.Http.Security;
using Fdw.Services.Connections.Http.Security.Types;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;

using Fdw.Services.Connections.Http;

using Fdw.Services.Connections.Http.Limits;

using Fdw.Services.Connections.Http.Results;

using Fdw.Services.Connections.Http.Commands;

using Fdw.Services.Connections.Http.Validation;

using Fdw.Services.Connections.Http.Protocols;


using ReferenceConnections.Http.Logging;

namespace ReferenceConnections.Http;

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
public sealed class HttpConnectionFactory : IHttpConnectionFactory
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
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _secretManagerProvider = secretManagerProvider ?? throw new ArgumentNullException(nameof(secretManagerProvider));
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
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _secretManagerProvider = null;
    }

    /// <inheritdoc/>
    // Why: this is the pure-construction contract. It NEVER resolves a secret — if the bound security
    // configuration demands one (WS-Security certificate, Basic/UsernameToken password, API key), it
    // fails loud. Callers whose connection needs a secret must use one of the async overloads below.
    public IGenericResult<IGenericConnection> Create(IGenericConfiguration configuration)
    {
        HttpConnectionFactoryLogger.TraceCreateGenericEntry(_logger, configuration?.GetType().Name ?? "null");

        if (configuration == null)
        {
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.ConfigurationNull(_logger));
        }

        // Why: After config-split DefaultConnectionProvider.CreateFromHeader passes the composed
        // ConnectionConfiguration header (with Configuration = HttpConnectionConfiguration).
        // Extract Name from the header and typed body from header.Configuration.
        if (configuration is ConnectionConfiguration header
            && header.Configuration is HttpConnectionConfiguration typedBody)
        {
            return CreateInternal(typedBody, header.Name);
        }

        if (configuration is HttpConnectionConfiguration httpConfig)
        {
            return CreateInternal(httpConfig, string.Empty);
        }

        return GenericResult<IGenericConnection>.Failure(
            HttpConnectionFactoryLogger.InvalidConfigurationType(_logger, configuration.GetType().Name));
    }

    /// <inheritdoc/>
    // Why: async path used by ConfigurationGateway during its connection bootstrap. Awaits
    // ISecretManager.Execute directly (secret resolution is inherently async — most secret stores
    // call external systems) using the supplied secretManager. Falls back to the sync Create when no
    // manager is supplied — which itself fails loud if the connection turns out to need one.
    public async Task<IGenericResult<IGenericConnection>> Create(
        IGenericConfiguration configuration,
        ISecretManager? secretManager,
        CancellationToken cancellationToken = default)
    {
        HttpConnectionFactoryLogger.TraceCreateGenericEntry(_logger, configuration?.GetType().Name ?? "null");

        if (configuration is null)
        {
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.ConfigurationNull(_logger));
        }

        if (configuration is ConnectionConfiguration header
            && header.Configuration is HttpConnectionConfiguration typedFromHeader)
        {
            return secretManager is null
                ? CreateInternal(typedFromHeader, header.Name)
                : await CreateAsyncInternal(typedFromHeader, header.Name, secretManager, cancellationToken).ConfigureAwait(false);
        }

        if (configuration is not HttpConnectionConfiguration httpCfg)
        {
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.InvalidConfigurationType(_logger, configuration.GetType().Name));
        }

        return secretManager is null
            ? CreateInternal(httpCfg, string.Empty)
            : await CreateAsyncInternal(httpCfg, string.Empty, secretManager, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    // Why: this factory reads the manager NAME from the authentication KVP and awaits resolution via
    // the secret-manager provider it was constructed with.
    public async Task<IGenericResult<IGenericConnection>> Create(
        IGenericConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (configuration is null)
            return GenericResult<IGenericConnection>.Failure(HttpConnectionFactoryLogger.ConfigurationNull(_logger));

        var (typedBody, name) = configuration switch
        {
            ConnectionConfiguration header when header.Configuration is HttpConnectionConfiguration body => (body, header.Name),
            HttpConnectionConfiguration flat => (flat, string.Empty),
            _ => (null, string.Empty),
        };
        if (typedBody is null)
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.InvalidConfigurationType(_logger, configuration.GetType().Name));

        var effectiveName = name.Length > 0 ? name : typedBody.ConnectionId.ToString();

        // Why: no bound security secret names => no credential required (e.g. AuthenticationType "None");
        // the sync pure-construction path builds it directly.
        var (_, certificateSecretName, passwordSecretName, apiKeySecretName) = ResolveSecurityRequirement(typedBody);
        if (string.IsNullOrWhiteSpace(certificateSecretName)
            && string.IsNullOrWhiteSpace(passwordSecretName)
            && string.IsNullOrWhiteSpace(apiKeySecretName))
        {
            return CreateInternal(typedBody, name);
        }

        // Why: the manager NAME is a key the secret-backed HttpAuthenticationTypes option declares in the KVP —
        // never a hardcoded "Default" guess. Resolve it through the FDW provider and await; a miss
        // fails loud.
        var secretManagerName = GetSecretManagerName(typedBody);
        if (string.IsNullOrEmpty(secretManagerName))
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.SecretManagerNotConfigured(_logger, effectiveName));

        if (_secretManagerProvider is null)
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.SecretManagerNotFound(_logger, effectiveName, secretManagerName));

        var managerResult = await _secretManagerProvider.Get(secretManagerName, cancellationToken).ConfigureAwait(false);
        if (!managerResult.IsSuccess || managerResult.Value is null)
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.SecretManagerNotFound(_logger, effectiveName, secretManagerName));

        return await CreateAsyncInternal(typedBody, name, managerResult.Value, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates an HTTP connection using the generic configuration interface with connection type specification.
    /// </summary>
    /// <param name="configuration">The configuration object.</param>
    /// <param name="connectionType">The connection type (must be "Http").</param>
    /// <returns>A result containing the created connection or failure information.</returns>
    public IGenericResult<IGenericConnection> Create(IGenericConfiguration configuration, string connectionType)
    {
        HttpConnectionFactoryLogger.TraceCreateWithTypeEntry(_logger, connectionType, configuration.GetType().Name);

        if (!string.Equals(connectionType, "Http", StringComparison.OrdinalIgnoreCase))
        {
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.UnsupportedConnectionType(_logger, connectionType));
        }

        return Create(configuration);
    }

    /// <summary>
    /// Creates an HTTP connection using the HttpConnectionConfiguration (direct typed-body path).
    /// </summary>
    /// <param name="configuration">The typed HTTP connection configuration.</param>
    /// <returns>A result containing the created connection or failure information.</returns>
    /// <remarks>
    /// Direct typed-body path — connection name is not available from the typed body after config-split.
    /// In production the composed-header path via <see cref="Create(IGenericConfiguration)"/> is used,
    /// which extracts the name from the parent <c>ConnectionConfiguration</c> header.
    /// No secret is resolved on this path — see <see cref="CreateInternal(HttpConnectionConfiguration, string)"/>.
    /// </remarks>
    public IGenericResult<IGenericConnection> Create(HttpConnectionConfiguration configuration)
        // Why: Direct typed-body path has no ConnectionConfiguration header to read Name from.
        // Pass string.Empty as connectionName; logs will show empty name for this path only.
        => CreateInternal(configuration, string.Empty);

    /// <summary>
    /// Core creation logic for the SYNC, pure-construction path — called from all sync public
    /// Create overloads with the connection name resolved from the header (or empty).
    /// </summary>
    // Why: the SYNC path never resolves a secret — secret resolution is async. If this connection's
    // bound security configuration demands a secret (cert/password/api-key name present), FAIL LOUD —
    // the caller must build it through one of the async Create overloads that supply a secret source.
    private IGenericResult<IGenericConnection> CreateInternal(HttpConnectionConfiguration configuration, string connectionName)
    {
        var effectiveName = connectionName.Length > 0 ? connectionName : configuration?.ConnectionId.ToString() ?? "null";
        HttpConnectionFactoryLogger.TraceCreateEntry(_logger, effectiveName);

        if (configuration == null)
        {
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.ConfigurationNull(_logger));
        }

        try
        {
            HttpConnectionFactoryLogger.CreatingConnection(_logger, connectionName);

            HttpConnectionFactoryLogger.TraceResolvingProtocol(_logger, configuration.Protocol, connectionName);
            var protocol = ResolveProtocol(configuration, connectionName);
            if (protocol == null)
            {
                return GenericResult<IGenericConnection>.Failure(
                    HttpConnectionFactoryLogger.ProtocolNotFound(_logger, connectionName, configuration.Protocol));
            }

            var (_, certificateSecretName, passwordSecretName, apiKeySecretName) = ResolveSecurityRequirement(configuration);
            if (!string.IsNullOrWhiteSpace(certificateSecretName)
                || !string.IsNullOrWhiteSpace(passwordSecretName)
                || !string.IsNullOrWhiteSpace(apiKeySecretName))
            {
                return GenericResult<IGenericConnection>.Failure(
                    HttpResultCodes.ByName("SecretManagerUnavailable"),
                    ResultDetails.Create().With("SecretManagerName", GetSecretManagerName(configuration) ?? string.Empty));
            }

            HttpConnectionFactoryLogger.TraceCreatingHttpClient(_logger, connectionName, configuration.BaseUrl);
            return AssembleConnection(configuration, connectionName, protocol, certificate: null, password: null, apiKey: null);
        }
        catch (Exception ex)
        {
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.CreationFailed(_logger, connectionName, ex.Message));
        }
    }

    /// <summary>
    /// Core creation logic for the ASYNC path — resolves any secrets the bound security configuration
    /// demands through the supplied <paramref name="secretManager"/> and builds the connection.
    /// </summary>
    private async Task<IGenericResult<IGenericConnection>> CreateAsyncInternal(
        HttpConnectionConfiguration configuration,
        string connectionName,
        ISecretManager secretManager,
        CancellationToken cancellationToken)
    {
        var effectiveName = connectionName.Length > 0 ? connectionName : configuration.ConnectionId.ToString();
        HttpConnectionFactoryLogger.TraceCreateEntry(_logger, effectiveName);

        try
        {
            HttpConnectionFactoryLogger.CreatingConnection(_logger, connectionName);

            HttpConnectionFactoryLogger.TraceResolvingProtocol(_logger, configuration.Protocol, connectionName);
            var protocol = ResolveProtocol(configuration, connectionName);
            if (protocol == null)
            {
                return GenericResult<IGenericConnection>.Failure(
                    HttpConnectionFactoryLogger.ProtocolNotFound(_logger, connectionName, configuration.Protocol));
            }

            var secretsResult = await ResolveSecretsIfNeeded(configuration, secretManager, cancellationToken).ConfigureAwait(false);
            if (!secretsResult.IsSuccess)
                return secretsResult.ToNewResult<IGenericConnection>();

            var (certificate, password, apiKey) = secretsResult.Value;

            HttpConnectionFactoryLogger.TraceCreatingHttpClient(_logger, connectionName, configuration.BaseUrl);
            return AssembleConnection(configuration, connectionName, protocol, certificate, password, apiKey);
        }
        catch (Exception ex)
        {
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.CreationFailed(_logger, connectionName, ex.Message));
        }
    }

    /// <summary>
    /// Creates an HTTP connection using the provided dependencies directly.
    /// </summary>
    /// <param name="configuration">The connection configuration.</param>
    /// <param name="httpClientFactory">The HTTP client factory for creating clients.</param>
    /// <param name="protocol">The HTTP protocol implementation.</param>
    /// <param name="loggerFactory">The logger factory for creating connection loggers.</param>
    /// <param name="secretManager">The secret manager for resolving security secrets (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the created connection or failure information.</returns>
    /// <remarks>
    /// <para>
    /// This overload accepts dependencies directly for bootstrap scenarios
    /// where the full DI container is not yet available.
    /// </para>
    /// </remarks>
    // Why: awaits ISecretManager.Execute directly.
    public async Task<IGenericResult<IGenericConnection>> Create(
        HttpConnectionConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IHttpProtocol protocol,
        ILoggerFactory loggerFactory,
        ISecretManager? secretManager = null,
        CancellationToken cancellationToken = default)
    {
        // Why: Direct deps-overload path — no ConnectionConfiguration header available, so connectionName
        // is unknown. Pass ConnectionId as a fallback identifier for log messages.
        var connectionName = configuration?.ConnectionId.ToString() ?? "null";
        HttpConnectionFactoryLogger.TraceCreateWithDepsEntry(_logger, connectionName);

        if (configuration == null)
        {
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.ConfigurationNull(_logger));
        }

        if (httpClientFactory == null)
        {
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.CreationFailed(_logger, connectionName, "IHttpClientFactory cannot be null"));
        }

        if (protocol == null)
        {
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.ProtocolNotFound(_logger, connectionName, configuration.Protocol));
        }

        if (loggerFactory == null)
        {
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.ContextNull(_logger, connectionName));
        }

        try
        {
            HttpConnectionFactoryLogger.CreatingConnection(_logger, connectionName);

            var secretsResult = await ResolveSecretsIfNeeded(configuration, secretManager, cancellationToken).ConfigureAwait(false);
            if (!secretsResult.IsSuccess)
                return secretsResult.ToNewResult<IGenericConnection>();

            var (certificate, password, apiKey) = secretsResult.Value;

            // Get and configure HTTP client
            var httpClient = configuration.UseMtls && certificate is not null
                ? CreateMtlsHttpClient(configuration, protocol, certificate)
                : CreateHttpClient(configuration, connectionName, httpClientFactory, protocol);

            if (configuration.UseMtls && certificate is not null)
            {
                HttpConnectionFactoryLogger.MtlsClientCreated(_logger, connectionName);
            }
            else if (configuration.UseMtls)
            {
                HttpConnectionFactoryLogger.MtlsCertificateRequiredButMissing(_logger, connectionName);
            }

            // Get a scoped logger for this connection instance
            var connectionLogger = loggerFactory.CreateLogger<HttpConnection>();

            var context = new HttpProtocolContext(configuration, loggerFactory, certificate, password, apiKey);

            // Create the connection instance
            var connection = new HttpConnection(
                connectionLogger,
                configuration,
                httpClient,
                protocol,
                context);

            HttpConnectionFactoryLogger.ConnectionCreated(_logger, connectionName, configuration.BaseUrl);

            return GenericResult<IGenericConnection>.Success(connection);
        }
        catch (Exception ex)
        {
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.CreationFailed(_logger, connectionName, ex.Message));
        }
    }

    /// <summary>
    /// Creates an HTTP connection using the configuration with connection type specification.
    /// </summary>
    /// <param name="configuration">The connection configuration.</param>
    /// <param name="connectionType">The connection type (must be "Http").</param>
    /// <returns>A result containing the created connection or failure information.</returns>
    public IGenericResult<IGenericConnection> Create(HttpConnectionConfiguration configuration, string connectionType)
    {
        // Why: connectionType overload has no header — name not available from typed body after config-split.
        HttpConnectionFactoryLogger.TraceCreateWithTypeEntry(_logger, connectionType, configuration?.ConnectionId.ToString() ?? "null");

        if (configuration == null)
        {
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.ConfigurationNull(_logger));
        }

        if (!string.Equals(connectionType, "Http", StringComparison.OrdinalIgnoreCase))
        {
            return GenericResult<IGenericConnection>.Failure(
                HttpConnectionFactoryLogger.UnsupportedConnectionType(_logger, connectionType));
        }

        return Create(configuration);
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
        if (protocol == null || string.Equals(protocol.Name, "_Empty", StringComparison.Ordinal))
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

    private static (HttpAuthenticationConfiguration? Instance, string? CertificateSecretName, string? PasswordSecretName, string? ApiKeySecretName)
        ResolveSecurityRequirement(HttpConnectionConfiguration configuration)
    {
        if (configuration.AdditionalProperties.Count == 0)
            return (null, null, null, null);

        var securityType = HttpAuthenticationTypes.ByName(configuration.AuthenticationType);
        if (securityType == null || string.IsNullOrEmpty(securityType.Name))
            return (null, null, null, null);

        var securityInstance = securityType.CreateInstance();
        securityInstance.BindFromValues(new Dictionary<string, string?>(configuration.AdditionalProperties, StringComparer.OrdinalIgnoreCase));

        var (certificateSecretName, passwordSecretName, apiKeySecretName) = GetSecretNames(securityInstance);
        return (securityInstance, certificateSecretName, passwordSecretName, apiKeySecretName);
    }

    /// <summary>Maps a bound security configuration to the secret names it demands.</summary>
    private static (string? CertificateSecretName, string? PasswordSecretName, string? ApiKeySecretName) GetSecretNames(
        HttpAuthenticationConfiguration security)
        => security switch
        {
            WsSecurityConfiguration ws => (ws.CertificateSecretName, null, null),
            UsernameTokenConfiguration ut => (null, ut.PasswordSecretName, null),
            ApiKeySecurityConfiguration ak => (null, null, ak.ApiKeySecretName),
            BasicSecurityConfiguration basic => (null, basic.PasswordSecretName, null),
            _ => (null, null, null),
        };

    /// <summary>
    /// Determines whether the bound security configuration demands a secret and, if so, resolves it
    /// through <paramref name="secretManager"/>. Returns a null-triple success when no secret is
    /// needed. Fails loud (never silently builds a credential-less connection) when a secret is
    /// needed but no secret manager was supplied.
    /// </summary>
    private static async Task<IGenericResult<(X509Certificate2? Certificate, string? Password, string? ApiKey)>> ResolveSecretsIfNeeded(
        HttpConnectionConfiguration configuration,
        ISecretManager? secretManager,
        CancellationToken cancellationToken)
    {
        var (_, certificateSecretName, passwordSecretName, apiKeySecretName) = ResolveSecurityRequirement(configuration);

        if (string.IsNullOrWhiteSpace(certificateSecretName)
            && string.IsNullOrWhiteSpace(passwordSecretName)
            && string.IsNullOrWhiteSpace(apiKeySecretName))
        {
            return GenericResult<(X509Certificate2?, string?, string?)>.Success((null, null, null));
        }

        if (secretManager is null)
        {
            return GenericResult<(X509Certificate2?, string?, string?)>.Failure(
                HttpResultCodes.ByName("SecretManagerUnavailable"),
                ResultDetails.Create().With("SecretManagerName", GetSecretManagerName(configuration) ?? string.Empty));
        }

        return await ResolveSecrets(certificateSecretName, passwordSecretName, apiKeySecretName, secretManager, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves the certificate/password/api-key secrets named by the bound security configuration,
    /// awaiting the supplied <paramref name="secretManager"/> directly.
    /// </summary>
    private static async Task<IGenericResult<(X509Certificate2? Certificate, string? Password, string? ApiKey)>> ResolveSecrets(
        string? certificateSecretName,
        string? passwordSecretName,
        string? apiKeySecretName,
        ISecretManager secretManager,
        CancellationToken cancellationToken)
    {
        X509Certificate2? certificate = null;
        string? password = null;
        string? apiKey = null;

        if (!string.IsNullOrWhiteSpace(certificateSecretName))
        {
            var certResult = await ResolveCertificate(certificateSecretName, secretManager, cancellationToken).ConfigureAwait(false);
            if (!certResult.IsSuccess)
                return certResult.ToNewResult<(X509Certificate2?, string?, string?)>();
            certificate = certResult.Value;
        }

        if (!string.IsNullOrWhiteSpace(passwordSecretName))
        {
            var passwordResult = await ResolveSecretString(passwordSecretName, secretManager, cancellationToken).ConfigureAwait(false);
            if (!passwordResult.IsSuccess)
                return passwordResult.ToNewResult<(X509Certificate2?, string?, string?)>();
            password = passwordResult.Value;
        }

        if (!string.IsNullOrWhiteSpace(apiKeySecretName))
        {
            var apiKeyResult = await ResolveSecretString(apiKeySecretName, secretManager, cancellationToken).ConfigureAwait(false);
            if (!apiKeyResult.IsSuccess)
                return apiKeyResult.ToNewResult<(X509Certificate2?, string?, string?)>();
            apiKey = apiKeyResult.Value;
        }

        return GenericResult<(X509Certificate2?, string?, string?)>.Success((certificate, password, apiKey));
    }

    /// <summary>
    /// Resolves a certificate from the secret manager.
    /// </summary>
    private static async Task<IGenericResult<X509Certificate2?>> ResolveCertificate(
        string secretName,
        ISecretManager secretManager,
        CancellationToken cancellationToken)
    {
        var secretResult = await secretManager.Execute(GetSecretManagerCommand.Latest(null, secretName), cancellationToken).ConfigureAwait(false);
        if (!secretResult.IsSuccess)
        {
            return secretResult.ToNewResult<X509Certificate2?>();
        }

        try
        {
            // Create certificate from bytes (assumes PFX/PKCS12 format)
            var cert = X509CertificateLoader.LoadPkcs12(secretResult.Value!.GetBinaryValue(), password: null);
            return GenericResult<X509Certificate2?>.Success(cert);
        }
        catch (Exception ex)
        {
            return GenericResult<X509Certificate2?>.Failure(
                HttpResultCodes.ByName("CertificateLoadFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
    }

    /// <summary>
    /// Resolves a string secret from the secret manager.
    /// </summary>
    private static async Task<IGenericResult<string?>> ResolveSecretString(
        string secretName,
        ISecretManager secretManager,
        CancellationToken cancellationToken)
    {
        var secretResult = await secretManager.Execute(GetSecretManagerCommand.Latest(null, secretName), cancellationToken).ConfigureAwait(false);
        if (!secretResult.IsSuccess)
        {
            return secretResult.ToNewResult<string?>();
        }

        return GenericResult<string?>.Success(secretResult.Value!.GetStringValue());
    }

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
        // Why: IHttpClientFactory.CreateClient uses the name as the named-options key.
        // After config-split, Name lives on the parent header; use connectionName from the factory
        // call site (extracted from the header). Falls back to ConnectionId string on direct paths.
        var clientName = connectionName.Length > 0 ? connectionName : configuration.ConnectionId.ToString();
        var client = httpClientFactory.CreateClient(clientName);

        // Configure client from configuration
        client.BaseAddress = new Uri(configuration.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(configuration.TimeoutSeconds);

        // Add default headers - use protocol's content type if not specified in config
        client.DefaultRequestHeaders.Clear();
        var contentType = configuration.ContentType ?? protocol.DefaultContentType;
        client.DefaultRequestHeaders.Add("Accept", contentType);
        client.DefaultRequestHeaders.Add("User-Agent", "Fdw-HTTP-Client/1.0");

        HttpConnectionFactoryLogger.ConfiguringHttpClient(_logger, connectionName, configuration.BaseUrl, configuration.TimeoutSeconds);

        return client;
    }

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
        client.BaseAddress = new Uri(configuration.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(configuration.TimeoutSeconds);
        client.DefaultRequestHeaders.Clear();
        var contentType = configuration.ContentType ?? protocol.DefaultContentType;
        client.DefaultRequestHeaders.Add("Accept", contentType);
        client.DefaultRequestHeaders.Add("User-Agent", "Fdw-HTTP-Client/1.0");
        return client;
    }

    #endregion

    #region IServiceFactory Implementation

    /// <inheritdoc/>
    IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        if (!result.IsSuccess || result.Value == null)
        {
            return result.ToNewResult<T>();
        }

        if (result.Value is T typedResult)
        {
            return GenericResult<T>.Success(typedResult);
        }

        return GenericResult<T>.Failure(
            HttpConnectionFactoryLogger.UnexpectedConnectionType(_logger, typeof(T).Name));
    }

    /// <inheritdoc/>
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        if (!result.IsSuccess || result.Value == null)
        {
            return result.ToNewResult<IGenericService>();
        }

        return GenericResult<IGenericService>.Success(result.Value);
    }

    /// <inheritdoc/>
    IGenericResult<IGenericConnection> IServiceFactory<IGenericConnection>.Create(IGenericConfiguration configuration)
    {
        return Create(configuration);
    }

    /// <inheritdoc/>
    IGenericResult<IGenericConnection> IServiceFactory<IGenericConnection, HttpConnectionConfiguration>.Create(
        HttpConnectionConfiguration configuration)
    {
        return Create(configuration);
    }

    #endregion
}
