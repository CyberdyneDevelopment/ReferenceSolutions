using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.Collections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using FdwReferenceSecretManagers.UserSecrets.Configuration;
using Fdw.Results;

namespace FdwReferenceSecretManagers.UserSecrets.Registration;

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
[Implementation(typeof(SecretManagerTypes), "UserSecrets")]
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
            defaultContainerName: "UserSecretsSecretManagerConfiguration")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<ISecretManagerProvider>();

            // Resolve factory from DI (registered in Phase 1)

            // Register factory instance with provider
            var factoryResult = provider.Register(Name, () => services.GetRequiredService<IUserSecretsSecretManagerProvider>());
            if (!factoryResult.IsSuccess) return factoryResult.ToNewResult<IHost>();

            // Why: Typed body providers are registered with the header provider (SecretManagerConfigurationProvider)
            // via discriminator dispatch. UserSecretsConfiguration no longer inherits
            // SecretManagerConfiguration — it implements ISecretManagerImplementationConfiguration directly.
            services.GetRequiredService<ISecretManagerConfigurationProvider>().Register(Name, services.GetRequiredService<IUserSecretsSecretManagerConfigurationProvider>());
    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

            builder.Services.AddOptions<List<UserSecretsConfiguration>>()
                .BindConfiguration("SecretManagers:UserSecrets");
    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {

            // Register the factory as singleton - DI handles all constructor dependencies
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IUserSecretsSecretManagerFactory), typeof(UserSecretsSecretManagerFactory), ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IUserSecretsSecretManagerProvider), typeof(UserSecretsSecretManagerProvider), ServiceLifetime.Singleton));
            builder.Services.AddSingleton<IUserSecretsSecretManagerConfigurationProvider, UserSecretsSecretManagerConfigurationProvider>(sp => new UserSecretsSecretManagerConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<UserSecretsSecretManagerConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                SecretManagerTypes.ConfigurationConnection, PathName, Container));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

    }



}