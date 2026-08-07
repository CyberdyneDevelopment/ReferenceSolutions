using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using ReferenceSecretManagers.AzureKeyVault.Logging;
using ReferenceSecretManagers.AzureKeyVault.Services;
using Microsoft.Extensions.Logging;
using ReferenceSecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.AzureKeyVault;

/// <summary>
/// Factory for creating <see cref="AzureKeyVaultSecretManager"/> instances.
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
/// All operations use the MessageLogging pattern: log AND return the message.
/// </para>
/// </remarks>
public sealed class AzureKeyVaultSecretManagerFactory : IAzureKeyVaultSecretManagerFactory
{
    private readonly ILogger<AzureKeyVaultSecretManagerFactory> _logger;
    private readonly ILogger<AzureKeyVaultSecretManager> _secretManagerLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultSecretManagerFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="secretManagerLogger">The logger for secret manager instances.</param>
    public AzureKeyVaultSecretManagerFactory(
        ILogger<AzureKeyVaultSecretManagerFactory> logger,
        ILogger<AzureKeyVaultSecretManager> secretManagerLogger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _secretManagerLogger = secretManagerLogger ?? throw new ArgumentNullException(nameof(secretManagerLogger));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<ISecretManager>> CreateSecretManager(AzureKeyVaultConfiguration configuration)
        // Why: Direct typed-body path has no SecretManagerConfiguration header — name not available.
        => CreateSecretManagerInternal(configuration, string.Empty);

    /// <inheritdoc/>
    public Task<IGenericResult<ISecretManager>> CreateSecretManager(IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return Task.FromResult<IGenericResult<ISecretManager>>(
                GenericResult<ISecretManager>.Failure(
                    AzureKeyVaultSecretManagerFactoryLogger.ConfigurationNull(_logger)));
        }

        // Why: After config-split DefaultServiceProvider.CreateFromParentConfig passes the composed
        // SecretManagerConfiguration header (with Configuration = AzureKeyVaultConfiguration).
        // Extract the typed body and name from the header.
        if (configuration is SecretManagerConfiguration header
            && header.Configuration is AzureKeyVaultConfiguration typedBody)
            return CreateSecretManagerInternal(typedBody, header.Name);

        if (configuration is AzureKeyVaultConfiguration azureConfig)
            return CreateSecretManagerInternal(azureConfig, string.Empty);

        return Task.FromResult<IGenericResult<ISecretManager>>(
            GenericResult<ISecretManager>.Failure(
                AzureKeyVaultSecretManagerFactoryLogger.InvalidConfigurationType(
                    _logger, configuration.GetType().Name)));
    }

    private Task<IGenericResult<ISecretManager>> CreateSecretManagerInternal(
        AzureKeyVaultConfiguration configuration, string secretManagerName)
    {
        if (configuration == null)
        {
            return Task.FromResult<IGenericResult<ISecretManager>>(
                GenericResult<ISecretManager>.Failure(
                    AzureKeyVaultSecretManagerFactoryLogger.ConfigurationNull(_logger)));
        }

        try
        {
            var effectiveName = secretManagerName.Length > 0 ? secretManagerName : configuration.SecretManagerId.ToString();
            AzureKeyVaultSecretManagerFactoryLogger.CreatingSecretManager(_logger, effectiveName);

            // Validate required configuration
            if (string.IsNullOrWhiteSpace(configuration.VaultUri))
            {
                return Task.FromResult<IGenericResult<ISecretManager>>(
                    GenericResult<ISecretManager>.Failure(
                        AzureKeyVaultSecretManagerFactoryLogger.VaultUriRequired(_logger, effectiveName)));
            }

            var service = new AzureKeyVaultSecretManager(_secretManagerLogger, configuration, effectiveName);

            AzureKeyVaultSecretManagerFactoryLogger.SecretManagerCreated(
                _logger, effectiveName, configuration.VaultUri);

            return Task.FromResult(GenericResult<ISecretManager>.Success(service));
        }
        catch (Exception ex)
        {
            var effectiveName = secretManagerName.Length > 0 ? secretManagerName : configuration.SecretManagerId.ToString();
            return Task.FromResult<IGenericResult<ISecretManager>>(
                GenericResult<ISecretManager>.Failure(
                    AzureKeyVaultSecretManagerFactoryLogger.CreationFailed(
                        _logger, effectiveName, ex.Message)));
        }
    }

    #region IServiceFactory Implementation

    // Note: IServiceFactory requires synchronous methods but our service creation is async.
    // These implementations use GetAwaiter().GetResult() which is intentional for this interface.
    // The VSTHRD002 warning is suppressed as there's no async alternative in the interface contract.

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits

    /// <inheritdoc/>
    public IGenericResult<ISecretManager> Create(AzureKeyVaultConfiguration configuration)
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
            AzureKeyVaultSecretManagerFactoryLogger.UnexpectedSecretManagerType(_logger, typeof(T).Name));
    }

#pragma warning restore VSTHRD002

    #endregion
}
