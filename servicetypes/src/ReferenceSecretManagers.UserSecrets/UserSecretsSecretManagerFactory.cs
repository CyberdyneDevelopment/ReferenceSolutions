using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.UserSecrets.Configuration;
using ReferenceSecretManagers.UserSecrets.Logging;
using ReferenceSecretManagers.UserSecrets.Services;
using Microsoft.Extensions.Logging;
using Fdw.Services.SecretManagers.UserSecrets.Commands;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.UserSecrets;

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
public sealed class UserSecretsSecretManagerFactory : IUserSecretsSecretManagerFactory
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
        // Why: Direct typed-body path has no SecretManagerConfiguration header — name not available.
        => CreateSecretManagerInternal(configuration, string.Empty);

    /// <inheritdoc/>
    public Task<IGenericResult<ISecretManager>> CreateSecretManager(IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return Task.FromResult<IGenericResult<ISecretManager>>(
                GenericResult<ISecretManager>.Failure(
                    UserSecretsFactoryLogger.ConfigurationNull(_logger)));
        }

        // Why: After config-split DefaultServiceProvider.CreateFromParentConfig passes the composed
        // SecretManagerConfiguration header (with Configuration = UserSecretsConfiguration).
        // Extract the typed body and name from the header.
        if (configuration is SecretManagerConfiguration header
            && header.Configuration is UserSecretsConfiguration typedBody)
            return CreateSecretManagerInternal(typedBody, header.Name);

        if (configuration is UserSecretsConfiguration directConfig)
            return CreateSecretManagerInternal(directConfig, string.Empty);

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
            var effectiveName = secretManagerName.Length > 0 ? secretManagerName : configuration.SecretManagerId.ToString();
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
            var effectiveName = secretManagerName.Length > 0 ? secretManagerName : configuration.SecretManagerId.ToString();
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
    public IGenericResult<ISecretManager> Create(UserSecretsConfiguration configuration)
    {
        return CreateSecretManager(configuration).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    IGenericResult<ISecretManager> IServiceFactory<ISecretManager>.Create(IGenericConfiguration configuration)
    {
        return CreateSecretManager(configuration).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var result = ((IServiceFactory<ISecretManager>)this).Create(configuration);

        if (!result.IsSuccess)
            return result.ToNewResult<IGenericService>();

        return GenericResult<IGenericService>.Success(result.Value!);
    }

    /// <inheritdoc/>
    IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
    {
        var result = ((IServiceFactory)this).Create(configuration);
        if (!result.IsSuccess)
            return result.ToNewResult<T>();

        if (result.Value is T typedService)
            return GenericResult<T>.Success(typedService);

        return GenericResult<T>.Failure(
            UserSecretsFactoryLogger.UnexpectedServiceType(_logger, typeof(T).Name));
    }

#pragma warning restore VSTHRD002

    #endregion
}
