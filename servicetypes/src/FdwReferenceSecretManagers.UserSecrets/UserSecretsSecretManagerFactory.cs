using System;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using FdwReferenceSecretManagers.UserSecrets.Configuration;
using FdwReferenceSecretManagers.UserSecrets.Logging;
using FdwReferenceSecretManagers.UserSecrets.Services;
using Microsoft.Extensions.Logging;
using Fdw.Services;

namespace FdwReferenceSecretManagers.UserSecrets;

/// <summary>
/// Factory for creating <see cref="UserSecretsSecretManager"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Registered as singleton in DI. Dependencies via constructor, config via Get().
/// Follows the two-phase registration pattern where:
/// </para>
/// <list type="bullet">
/// <item><description>Phase 1: Factory and its dependencies are registered with DI</description></item>
/// <item><description>Phase 2: Factory is resolved from DI and registered with the provider</description></item>
/// </list>
/// <para>
/// This factory uses the MessageLogging pattern where all operations are logged AND
/// the log message is returned in the result. Every failure mode has an explicit
/// MessageLogging method defined in <see cref="UserSecretsFactoryLogger"/>.
/// </para>
/// </remarks>
public sealed class UserSecretsSecretManagerFactory : ServiceFactoryBase<ISecretManager, UserSecretsConfiguration>, IUserSecretsSecretManagerFactory
{
    private readonly ILogger<UserSecretsSecretManagerFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSecretsSecretManagerFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="loggerFactory">The logger factory for creating service loggers.</param>
    public UserSecretsSecretManagerFactory(
        ILogger<UserSecretsSecretManagerFactory> logger,
        ILoggerFactory loggerFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<ISecretManager>> CreateSecretManager(UserSecretsConfiguration configuration)
        => CreateSecretManagerInternal(configuration, configuration.Name);

    /// <inheritdoc/>
    public Task<IGenericResult<ISecretManager>> CreateSecretManager(IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return Task.FromResult<IGenericResult<ISecretManager>>(
                GenericResult<ISecretManager>.Failure(
                    UserSecretsFactoryLogger.ConfigurationNull(_logger)));
        }

        if (configuration is UserSecretsConfiguration typed)
            return CreateSecretManagerInternal(typed, typed.Name);

        return Task.FromResult<IGenericResult<ISecretManager>>(
            GenericResult<ISecretManager>.Failure(
                UserSecretsFactoryLogger.InvalidConfigurationType(
                    _logger,
                    nameof(UserSecretsConfiguration),
                    configuration.GetType().Name)));
    }

    private Task<IGenericResult<ISecretManager>> CreateSecretManagerInternal(
        UserSecretsConfiguration configuration, string secretManagerName)
    {
        if (configuration == null)
        {
            return Task.FromResult<IGenericResult<ISecretManager>>(
                GenericResult<ISecretManager>.Failure(
                    UserSecretsFactoryLogger.ConfigurationNull(_logger)));
        }

        try
        {
            var effectiveName = secretManagerName.Length > 0 ? secretManagerName : configuration.Id.ToString();
            UserSecretsFactoryLogger.CreatingSecretManager(_logger, effectiveName);

            // Get a scoped logger for the secret manager instance
            var instanceLogger = _loggerFactory.CreateLogger<UserSecretsSecretManager>();

            // Get the secret manager instance
            var service = new UserSecretsSecretManager(instanceLogger, configuration, effectiveName);

            UserSecretsFactoryLogger.SecretManagerCreated(_logger, effectiveName, configuration.UserSecretsId ?? "default");

            return Task.FromResult(GenericResult<ISecretManager>.Success(service));
        }
        catch (Exception ex)
        {
            // Catch, log, return - never rethrow
            var effectiveName = secretManagerName.Length > 0 ? secretManagerName : configuration.Id.ToString();
            return Task.FromResult<IGenericResult<ISecretManager>>(
                GenericResult<ISecretManager>.Failure(
                    UserSecretsFactoryLogger.CreationFailed(_logger, effectiveName, ex.Message)));
        }
    }

    #region IServiceFactory Implementation

    // Note: IServiceFactory requires synchronous methods but our service creation is async.
    // These implementations use GetAwaiter().GetResult() which is intentional for this interface.
    // The VSTHRD002 warning is suppressed as there's no async alternative in the interface contract.

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits

    /// <inheritdoc/>
    public override IGenericResult<ISecretManager> Create(UserSecretsConfiguration configuration)
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
