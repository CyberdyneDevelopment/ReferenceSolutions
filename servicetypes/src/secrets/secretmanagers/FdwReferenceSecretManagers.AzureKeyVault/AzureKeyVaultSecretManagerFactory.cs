using System;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using FdwReferenceSecretManagers.AzureKeyVault.Configuration;
using FdwReferenceSecretManagers.AzureKeyVault.Logging;
using FdwReferenceSecretManagers.AzureKeyVault.Services;
using Microsoft.Extensions.Logging;
using Fdw.Services;

namespace FdwReferenceSecretManagers.AzureKeyVault;

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
public sealed class AzureKeyVaultSecretManagerFactory : ServiceFactoryBase<ISecretManager, AzureKeyVaultConfiguration>, IAzureKeyVaultSecretManagerFactory
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
        // Why the null-conditional: CreateSecretManagerInternal's own null check is what should answer
        // a null configuration -- reading .Name here first would NRE before that check ever runs.
        => CreateSecretManagerInternal(configuration, configuration?.Name!);

    /// <inheritdoc/>
    public Task<IGenericResult<ISecretManager>> CreateSecretManager(IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return Task.FromResult<IGenericResult<ISecretManager>>(
                GenericResult<ISecretManager>.Failure(
                    AzureKeyVaultSecretManagerFactoryLogger.ConfigurationNull(_logger)));
        }

        if (configuration is AzureKeyVaultConfiguration typed)
            return CreateSecretManagerInternal(typed, typed.Name);

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
            var effectiveName = secretManagerName.Length > 0 ? secretManagerName : configuration.Id.ToString();
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
            var effectiveName = secretManagerName.Length > 0 ? secretManagerName : configuration.Id.ToString();
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
    public override IGenericResult<ISecretManager> Create(AzureKeyVaultConfiguration configuration)
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
