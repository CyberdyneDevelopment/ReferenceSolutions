using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using Fdw.Services;
using Fdw;
using Fdw.Results;

namespace ReferenceSecretManagers.EnvironmentVariable;

/// <summary>
/// Service type definition for Environment Variable secret manager.
/// Handles two-phase registration lifecycle for environment variable-based secret retrieval.
/// </summary>
/// <remarks>
/// <para>
/// This service type uses the two-phase registration pattern:
/// <list type="bullet">
/// <item><description>Phase 1: Register factory and dependencies with main DI container</description></item>
/// <item><description>Phase 2: Resolve factory from DI and register with provider</description></item>
/// </list>
/// </para>
/// <para>
/// Configuration is loaded from "SecretManagers:{Name}" sections in appsettings.json:
/// <code>
/// {
///   "SecretManagers": {
///     "EnvironmentVariable": {
///       "Prefix": "APP_",
///       "CaseSensitive": false,
///       "Separator": "__",
///       "StripPrefix": true
///     }
///   }
/// }
/// </code>
/// </para>
/// <para>
/// This is a read-only secret manager that retrieves secrets from environment variables.
/// It does not support setting or deleting secrets as environment variables are typically
/// managed externally (e.g., by the operating system, container orchestration, or CI/CD pipelines).
/// </para>
/// </remarks>
[ServiceTypeOption(typeof(SecretManagerTypes), "EnvironmentVariable")]
public sealed class EnvironmentVariableSecretManagerType
    : SecretManagerTypeBase<ISecretManager, IEnvironmentVariableSecretManagerFactory, EnvironmentVariableConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariableSecretManagerType"/> class.
    /// </summary>
    public EnvironmentVariableSecretManagerType()
        : base(
            name: "EnvironmentVariable",
            sectionName: "EnvironmentVariable",
            displayName: "Environment Variable",
            description: "Secret manager that reads secrets from environment variables with support for prefixes and key transformation",
            supportedSecretStores: ["EnvironmentVariable"],
            supportedSecretTypes: ["Password", "ConnectionString", "ApiKey", "Token"],
            supportsRotation: false,      // Cannot automatically rotate env vars
            supportsVersioning: false,    // No version support in env vars
            supportsSoftDelete: false,    // Cannot soft delete env vars
            supportsAccessPolicies: false, // No access policy support
            maxSecretSizeBytes: 32767,    // Typical max env var size on most systems
            defaultContainerName: "EnvironmentVariableSecretManager")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>();

            // Resolve factory from DI (registered in Phase 1)
            var factory = services.GetRequiredService<IEnvironmentVariableSecretManagerFactory>();

            // Register factory instance with provider
            var factoryResult = provider.Register(Name, factory);
            if (!factoryResult.IsSuccess) return factoryResult.ToNewResult<IHost>();

            // Why: Typed body providers are registered with the header provider (SecretManagerConfigurationProvider)
            // via discriminator dispatch. EnvironmentVariableConfiguration no longer inherits
            // SecretManagerConfiguration — it implements ISecretManagerConfiguration directly.
            // The generic overload wraps via ConfigurationProviderAdapter so the marker-interface dict accepts it.
            var headerProvider = services.GetRequiredService<SecretManagerConfigurationProvider>();
            var configProvider = services.GetRequiredService<DefaultConfigurationProvider<EnvironmentVariableConfiguration, EnvironmentVariableConfigurationCommand>>();
            headerProvider.Register<EnvironmentVariableConfiguration>(Name, configProvider);
    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {

            // No special infrastructure dependencies needed for Environment Variable
            // It uses Environment.GetEnvironmentVariable directly

            // Register the factory as singleton - DI handles all constructor dependencies
            builder.Services.AddSingleton<IEnvironmentVariableSecretManagerFactory, EnvironmentVariableSecretManagerFactory>();

            // Why: Lazy<IDataGateway> defers cfg resolution until first runtime query, avoiding
            // circular dependency with the DataGateway that hasn't been built yet at registration time.
            // dataStoreName flows from TypeCollection.Configure() so "ConfigurationDb" is never hardcoded here.
            builder.Services.AddSingleton(sp => new DefaultConfigurationProvider<EnvironmentVariableConfiguration, EnvironmentVariableConfigurationCommand>(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<DefaultConfigurationProvider<EnvironmentVariableConfiguration, EnvironmentVariableConfigurationCommand>>(),
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                dataStoreName,
                pathName,
                new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

            // Why: RegisterFactory (below) requires SecretManagerConfigurationProvider (the shared header
            // provider for the whole SecretManager domain) to already be registered. TryAddSingleton inside
            // RegisterDomainConfiguration makes this idempotent — every secret manager option calls it, first
            // registration wins.
            SecretManagerConfigurationProvider.RegisterDomainConfiguration(builder.Services);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

    }



}
