using System;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using FdwReferenceSecretManagers.EnvironmentVariable.Configuration;
using FdwReferenceSecretManagers.EnvironmentVariable.Logging;
using FdwReferenceSecretManagers.EnvironmentVariable.Services;
using Microsoft.Extensions.Logging;
using Fdw.Services;

namespace FdwReferenceSecretManagers.EnvironmentVariable;

/// <summary>
/// Factory for creating <see cref="EnvironmentVariableSecretManager"/> instances.
/// </summary>
/// <remarks>
/// Registered as singleton in DI. Dependencies via constructor, config via Get().
/// Follows the two-phase registration pattern where:
/// - Phase 1: Factory and its dependencies are registered with DI
/// - Phase 2: Factory is resolved from DI and registered with the provider
/// </remarks>
public sealed class EnvironmentVariableSecretManagerFactory : ServiceFactoryBase<ISecretManager, EnvironmentVariableConfiguration>, IEnvironmentVariableSecretManagerFactory
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
        // Why the null-conditional: CreateSecretManagerInternal's own null check is what should answer
        // a null configuration -- reading .Name here first would NRE before that check ever runs.
        => CreateSecretManagerInternal(configuration, configuration?.Name!);

    /// <inheritdoc/>
    public System.Threading.Tasks.Task<IGenericResult<ISecretManager>> CreateSecretManager(IGenericConfiguration configuration)
    {
        EnvironmentVariableLogger.TraceCreateSecretManagerGenericEntry(_logger);

        if (configuration is EnvironmentVariableConfiguration typed)
            return CreateSecretManagerInternal(typed, typed.Name);

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
            var effectiveName = secretManagerName.Length > 0 ? secretManagerName : configuration.Id.ToString();
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
    public override IGenericResult<ISecretManager> Create(EnvironmentVariableConfiguration configuration)
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
