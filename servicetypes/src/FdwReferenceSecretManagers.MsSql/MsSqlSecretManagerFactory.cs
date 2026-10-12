using System;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Security.Hashing;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using FdwReferenceSecretManagers.MsSql.Configuration;
using FdwReferenceSecretManagers.MsSql.Logging;
using FdwReferenceSecretManagers.MsSql.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services;

namespace FdwReferenceSecretManagers.MsSql;

/// <summary>
/// Factory for creating <see cref="MsSqlSecretManager"/> instances.
/// </summary>
/// <remarks>
/// Reads/writes the secret manager's tables through the injected <see cref="IConfigurationGateway"/>
/// (PlatformConfiguration) instead of a hand-built <c>IDataConnection</c> — this eliminates the bootstrap
/// connection that previously failed at runtime with "SecretManagerProvider is not available".
/// </remarks>
public sealed class MsSqlSecretManagerFactory : ServiceFactoryBase<ISecretManager, MsSqlSecretManagerConfiguration>, IMsSqlSecretManagerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<MsSqlSecretManagerFactory> _logger;
    private readonly IConfigurationGatewayProvider _gatewayProvider;
    private readonly string _dataStoreName;
    private readonly IPasswordHasher? _passwordHasher;
    private readonly IPersonalAccessTokenHasher? _tokenHasher;
    private readonly IPersonalAccessTokenGenerator? _tokenGenerator;
    private readonly string? _hmacKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlSecretManagerFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory for creating service loggers.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the named connection.</param>
    /// <param name="dataStoreName">The DataStore name secret/user tables are addressed under (e.g. "PlatformConfiguration").</param>
    /// <param name="passwordHasher">Optional password hasher for credential verification.</param>
    /// <param name="tokenHasher">Optional PAT hasher for API key verification.</param>
    /// <param name="tokenGenerator">Optional PAT generator for API key creation.</param>
    /// <param name="hmacKey">Optional HMAC key for API key hashing operations.</param>
    public MsSqlSecretManagerFactory(
        ILoggerFactory? loggerFactory,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        IPasswordHasher? passwordHasher = null,
        IPersonalAccessTokenHasher? tokenHasher = null,
        IPersonalAccessTokenGenerator? tokenGenerator = null,
        string? hmacKey = null)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<MsSqlSecretManagerFactory>();
        _gatewayProvider = gatewayProvider ?? throw new ArgumentNullException(nameof(gatewayProvider));
        _dataStoreName = dataStoreName ?? throw new ArgumentNullException(nameof(dataStoreName));
        _passwordHasher = passwordHasher;
        _tokenHasher = tokenHasher;
        _tokenGenerator = tokenGenerator;
        _hmacKey = hmacKey;
    }

    /// <inheritdoc/>
    public Task<IGenericResult<ISecretManager>> CreateSecretManager(MsSqlSecretManagerConfiguration configuration)
        => CreateSecretManagerInternal(configuration, configuration.Name);

    private Task<IGenericResult<ISecretManager>> CreateSecretManagerInternal(
        MsSqlSecretManagerConfiguration configuration, string secretManagerName)
    {
        MsSqlSecretManagerLogger.TraceCreateSecretManagerEntry(_logger);

        var effectiveName = secretManagerName.Length > 0 ? secretManagerName : configuration.Id.ToString();

        try
        {
            MsSqlSecretManagerLogger.CreatingSecretManager(_logger, effectiveName);

            var gateway = _gatewayProvider.Get(_dataStoreName);
            if (gateway.IsFailure)
                return Task.FromResult(gateway.ToNewResult<ISecretManager>());

            var serviceLogger = _loggerFactory.CreateLogger<MsSqlSecretManager>();
            var service = new MsSqlSecretManager(
                serviceLogger, configuration, gateway.Value!, _dataStoreName, effectiveName,
                _passwordHasher, _tokenHasher, _tokenGenerator, _hmacKey);

            MsSqlSecretManagerLogger.SecretManagerCreated(_logger, effectiveName);
            return Task.FromResult(GenericResult<ISecretManager>.Success(service));
        }
        catch (Exception ex)
        {
            return Task.FromResult<IGenericResult<ISecretManager>>(
                GenericResult<ISecretManager>.Failure(MsSqlSecretManagerLogger.CreationFailed(_logger, ex.Message)));
        }
    }

    /// <inheritdoc/>
    public System.Threading.Tasks.Task<IGenericResult<ISecretManager>> CreateSecretManager(IGenericConfiguration configuration)
    {
        MsSqlSecretManagerLogger.TraceCreateSecretManagerGenericEntry(_logger);

        if (configuration is MsSqlSecretManagerConfiguration typed)
            return CreateSecretManagerInternal(typed, typed.Name);

        var actualType = configuration?.GetType().Name ?? "null";
        return System.Threading.Tasks.Task.FromResult<IGenericResult<ISecretManager>>(
            GenericResult<ISecretManager>.Failure(
                MsSqlSecretManagerLogger.InvalidConfigurationType(
                    _logger,
                    nameof(MsSqlSecretManagerConfiguration),
                    actualType)));
    }

    #region IServiceFactory Implementation

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits

    /// <inheritdoc/>
    public override IGenericResult<ISecretManager> Create(MsSqlSecretManagerConfiguration configuration)
    {
        return CreateSecretManager(configuration).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    IGenericResult<ISecretManager> IServiceFactory<ISecretManager>.Create(IGenericConfiguration configuration)
    {
        return CreateSecretManager(configuration).GetAwaiter().GetResult();
    }



#pragma warning restore VSTHRD002

    #endregion
}
