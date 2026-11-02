using Fdw.Services.Connections.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.Results;

namespace FdwReferenceConnections.Http;

/// <summary>
/// Builds HTTP connections, fetching the certificate, password and api key the connection's
/// security configuration names before handing them to the factory.
/// </summary>
/// <remarks>
/// Http is why the resolved dependency could not live on the domain-wide contract: it needs three
/// secrets, one of which is an X509Certificate2 rather than a string. Because an implementation
/// provider may know its implementation, it can ask for exactly those and pass them typed.
/// </remarks>
public sealed class HttpConnectionProvider
    : ImplementationServiceProviderBase<IGenericConnection, IConnectionImplementationConfiguration>,
      IHttpConnectionProvider
{
    private readonly IHttpConnectionFactory _factory;
    private readonly ISecretManagerProvider _secretManagerProvider;
    private readonly ILogger<HttpConnectionProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="HttpConnectionProvider"/> class.</summary>
    /// <param name="factory">Builds the connection once its secrets are in hand.</param>
    /// <param name="secretManagerProvider">Resolves the store the configuration names.</param>
    /// <param name="logger">The logger for this provider.</param>
    public HttpConnectionProvider(
        IHttpConnectionFactory factory,
        ISecretManagerProvider secretManagerProvider,
        ILogger<HttpConnectionProvider>? logger = null)
    {
        _factory = factory;
        _secretManagerProvider = secretManagerProvider;
        _logger = logger ?? NullLogger<HttpConnectionProvider>.Instance;
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<IGenericConnection>> Create(
        IConnectionImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not HttpConnectionConfiguration typed)
            return _factory.Create(configuration);

        var required = _factory.RequiredSecrets(typed);
        if (!required.IsSuccess)
            return required.ToNewResult<IGenericConnection>();

        var (certificateName, passwordName, apiKeyName, managerName) = required.Value;
        if (certificateName is null && passwordName is null && apiKeyName is null)
            return _factory.Create(typed, certificate: null, password: null, apiKey: null);

        if (string.IsNullOrWhiteSpace(managerName))
            return GenericResult<IGenericConnection>.Failure(
                HttpResultCodes.ByName("SecretManagerUnavailable"),
                ResultDetails.Create(
                    "Connection", typed.Name,
                    "Reason", "the configuration names secrets but no SecretManagerName to read them from"));

        var manager = await _secretManagerProvider.Get(managerName!, cancellationToken).ConfigureAwait(false);
        if (!manager.IsSuccess || manager.Value is null)
            return manager.ToNewResult<IGenericConnection>();

        var certificate = await Secret(manager.Value, certificateName, cancellationToken).ConfigureAwait(false);
        if (!certificate.IsSuccess) return certificate.ToNewResult<IGenericConnection>();

        var password = await Secret(manager.Value, passwordName, cancellationToken).ConfigureAwait(false);
        if (!password.IsSuccess) return password.ToNewResult<IGenericConnection>();

        var apiKey = await Secret(manager.Value, apiKeyName, cancellationToken).ConfigureAwait(false);
        if (!apiKey.IsSuccess) return apiKey.ToNewResult<IGenericConnection>();

        return _factory.Create(
            typed,
            certificate.Value is null ? null : X509CertificateLoader.LoadPkcs12(
                System.Convert.FromBase64String(certificate.Value), password.Value),
            password.Value,
            apiKey.Value);
    }

    // One read per named secret; an unnamed one is simply not read.
    private static async Task<IGenericResult<string?>> Secret(
        ISecretManager manager,
        string? keyName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(keyName))
            return GenericResult<string?>.Success(null);

        var result = await manager
            .Execute(GetSecretManagerCommand.Latest(null, keyName!), cancellationToken)
            .ConfigureAwait(false);
        return result.IsSuccess && result.Value is not null
            ? GenericResult<string?>.Success(result.Value.GetStringValue())
            : result.ToNewResult<string?>();
    }
}
