using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
using Fdw.Services.SecretManagers.UserSecrets.Commands;
using Fdw.Services.SecretManagers.UserSecrets.Configuration;
using Fdw.Services;
using Fdw;

namespace ReferenceSecretManagers.UserSecrets;

/// <summary>
/// Service type definition for .NET User Secrets secret manager.
/// Handles two-phase registration lifecycle for User Secrets integration.
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
///     "UserSecrets": {
///       "UserSecretsId": "79a3edd0-2092-40a2-a04d-dcb46d5ca9ed",
///       "ReloadOnChange": true,
///       "Optional": true
///     }
///   }
/// }
/// </code>
/// </para>
/// <para>
/// User Secrets is a read-only secret manager for development scenarios.
/// It supports only GetSecret and ListSecrets operations.
/// </para>
/// </remarks>
[ServiceTypeOption(typeof(SecretManagerTypes), "UserSecrets")]
public sealed class UserSecretsSecretManagerType
    : SecretManagerTypeBase<ISecretManager, IUserSecretsSecretManagerFactory, UserSecretsConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSecretsSecretManagerType"/> class.
    /// </summary>
    public UserSecretsSecretManagerType()
        : base(
            name: "UserSecrets",
            sectionName: "UserSecrets",
            displayName: "User Secrets",
            description: ".NET User Secrets for local development secret management (read-only)",
            supportedSecretStores: ["UserSecrets"],
            supportedSecretTypes: ["Password", "ConnectionString", "ApiKey", "Token"],
            supportsRotation: false,       // Read-only
            supportsVersioning: false,     // No versioning support
            supportsSoftDelete: false,     // Read-only
            supportsAccessPolicies: false, // No access control
            maxSecretSizeBytes: 1048576,   // Limited by JSON file size (1 MB practical limit)
            supportsBatchOperations: true, // Can list all secrets
            supportsExpiration: false,     // No expiration support
            supportsTagging: false,        // No metadata support
            priority: 10,                  // Low priority - development only
            defaultContainerName: "UserSecretsSecretManager")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>();

            // Resolve factory from DI (registered in Phase 1)
            var factory = services.GetRequiredService<IUserSecretsSecretManagerFactory>();

            // Register factory instance with provider
            var factoryResult = provider.Register(Name, factory);
            if (!factoryResult.IsSuccess) return host;

            // Why: IOptionsMonitor<List<UserSecretsConfiguration>> now contains ctrl data
            // (from SqlServerConfigurationProvider) and appsettings.json data. cfg data is served
            // by the provider's user cache via DataGateway.
            var configLogger = services.GetService<ILoggerFactory>()
                ?.CreateLogger<DefaultConfigurationProvider<UserSecretsConfiguration, UserSecretsConfigurationCommand>>()
                ?? NullLogger<DefaultConfigurationProvider<UserSecretsConfiguration, UserSecretsConfigurationCommand>>.Instance;
            var lazyGateway = services.GetRequiredService<Lazy<IConfigurationGateway>>();
            var configProvider = new DefaultConfigurationProvider<UserSecretsConfiguration, UserSecretsConfigurationCommand>(
                configLogger,
                lazyGateway,
                "ConfigurationDb",
                "sec",
                new Lazy<ICacheInvalidator?>(() => services.GetService<ICacheInvalidator>()));
            // Why: Typed body providers are registered with the header provider (SecretManagerConfigurationProvider)
            // via discriminator dispatch. UserSecretsConfiguration no longer inherits
            // SecretManagerConfiguration — it implements ISecretManagerConfiguration directly.
            var headerProvider = services.GetRequiredService<SecretManagerConfigurationProvider>();
            headerProvider.RegisterTypedProvider<UserSecretsConfiguration>(Name, configProvider);
    
            return host;
        });

        Configuration(builder =>
        {

            builder.Services.AddOptions<List<UserSecretsConfiguration>>()
                .BindConfiguration("SecretManagers:UserSecrets");
    
            return builder;
        });

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {

            // Register the factory as singleton - DI handles all constructor dependencies
            builder.Services.AddSingleton<IUserSecretsSecretManagerFactory, UserSecretsSecretManagerFactory>();

            // Why: RegisterFactory (below) requires SecretManagerConfigurationProvider (the shared header
            // provider for the whole SecretManager domain) to already be registered. TryAddSingleton inside
            // RegisterDomainConfiguration makes this idempotent — every secret manager option calls it, first
            // registration wins.
            SecretManagerConfigurationProvider.RegisterDomainConfiguration(builder.Services);
            return builder;
    
        });

    }



}
