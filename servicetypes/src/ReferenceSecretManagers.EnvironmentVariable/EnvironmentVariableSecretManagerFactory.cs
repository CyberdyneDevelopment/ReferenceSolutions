using System;
using System.Linq;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using ReferenceSecretManagers.EnvironmentVariable.Logging;
using ReferenceSecretManagers.EnvironmentVariable.Services;
using Microsoft.Extensions.Logging;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.EnvironmentVariable;

/// <summary>
/// Factory for creating <see cref="EnvironmentVariableSecretManager"/> instances.
/// </summary>
/// <remarks>
/// Registered as singleton in DI. Dependencies via constructor, config via Get().
/// Follows the two-phase registration pattern where:
/// - Phase 1: Factory and its dependencies are registered with DI
/// - Phase 2: Factory is resolved from DI and registered with the provider
/// </remarks>
public sealed class EnvironmentVariableSecretManagerFactory : IEnvironmentVariableSecretManagerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<EnvironmentVariableSecretManagerFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariableSecretManagerFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory for creating service loggers.</param>
    public EnvironmentVariableSecretManagerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<EnvironmentVariableSecretManagerFactory>();
    }

    /// <inheritdoc/>
    public System.Threading.Tasks.Task<IGenericResult<ISecretManager>> CreateSecretManager(EnvironmentVariableConfiguration configuration)
        // Why: Direct typed-body path has no SecretManagerConfiguration header — name not available.
        => CreateSecretManagerInternal(configuration, string.Empty);

    /// <inheritdoc/>
    public System.Threading.Tasks.Task<IGenericResult<ISecretManager>> CreateSecretManager(IGenericConfiguration configuration)
    {
        EnvironmentVariableLogger.TraceCreateSecretManagerGenericEntry(_logger);

        // Why: After config-split DefaultServiceProvider.CreateFromParentConfig passes the composed
        // SecretManagerConfiguration header (with Configuration = EnvironmentVariableConfiguration).
        // Extract the typed body and name from the header.
        if (configuration is SecretManagerConfiguration header
            && header.Configuration is EnvironmentVariableConfiguration typedBody)
            return CreateSecretManagerInternal(typedBody, header.Name);

        if (configuration is EnvironmentVariableConfiguration directConfig)
            return CreateSecretManagerInternal(directConfig, string.Empty);

        var actualType = configuration?.GetType().Name ?? "null";
        return System.Threading.Tasks.Task.FromResult<IGenericResult<ISecretManager>>(
            GenericResult<ISecretManager>.Failure(
                EnvironmentVariableLogger.InvalidConfigurationType(
                    _logger,
                    nameof(EnvironmentVariableConfiguration),
                    actualType)));
    }

    private System.Threading.Tasks.Task<IGenericResult<ISecretManager>> CreateSecretManagerInternal(
        EnvironmentVariableConfiguration configuration, string secretManagerName)
    {
        EnvironmentVariableLogger.TraceCreateSecretManagerEntry(_logger);

        if (configuration == null)
        {
            return System.Threading.Tasks.Task.FromResult<IGenericResult<ISecretManager>>(
                GenericResult<ISecretManager>.Failure(EnvironmentVariableLogger.ConfigurationNull(_logger)));
        }

        try
        {
            var effectiveName = secretManagerName.Length > 0 ? secretManagerName : configuration.SecretManagerId.ToString();
            EnvironmentVariableLogger.CreatingSecretManager(_logger, effectiveName);

            var serviceLogger = _loggerFactory.CreateLogger<EnvironmentVariableSecretManager>();
            var service = new EnvironmentVariableSecretManager(serviceLogger, configuration, effectiveName);

            EnvironmentVariableLogger.SecretManagerCreated(_logger, effectiveName);
            return System.Threading.Tasks.Task.FromResult(GenericResult<ISecretManager>.Success(service));
        }
        catch (Exception ex)
        {
            return System.Threading.Tasks.Task.FromResult<IGenericResult<ISecretManager>>(
                GenericResult<ISecretManager>.Failure(EnvironmentVariableLogger.CreationFailed(_logger, ex.Message)));
        }
    }

    #region IServiceFactory Implementation

    // Note: IServiceFactory requires synchronous methods but our service creation is async.
    // These implementations use GetAwaiter().GetResult() which is intentional for this interface.
    // The VSTHRD002 warning is suppressed as there's no async alternative in the interface contract.

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits

    /// <inheritdoc/>
    public IGenericResult<ISecretManager> Create(EnvironmentVariableConfiguration configuration)
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

        return GenericResult<T>.Failure(EnvironmentVariableLogger.ServiceTypeMismatch(_logger, typeof(T).Name));
    }

#pragma warning restore VSTHRD002

    #endregion
}
