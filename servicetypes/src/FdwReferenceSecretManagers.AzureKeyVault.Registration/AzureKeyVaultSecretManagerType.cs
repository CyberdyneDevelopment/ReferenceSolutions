using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.Collections;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using FdwReferenceSecretManagers.AzureKeyVault.Configuration;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace FdwReferenceSecretManagers.AzureKeyVault.Registration;

/// <summary>
/// Service type definition for Azure Key Vault secret manager.
/// Handles two-phase registration lifecycle for Azure Key Vault integration.
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
///     "AzureKeyVault": {
///       "VaultUri": "https://myvault.vault.azure.net/",
///       "AuthenticationMethod": "ManagedIdentity",
///       "TenantId": "tenant-id",
///       "ClientId": "client-id"
///     }
///   }
/// }
/// </code>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(SecretManagerTypes), "AzureKeyVault")]
public sealed class AzureKeyVaultSecretManagerType
    : SecretManagerTypeBase<ISecretManager, IAzureKeyVaultSecretManagerFactory, AzureKeyVaultConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultSecretManagerType"/> class.
    /// </summary>
    public AzureKeyVaultSecretManagerType()
        : base(
            name: "AzureKeyVault",
            sectionName: "AzureKeyVault",
            displayName: "Azure Key Vault",
            description: "Microsoft Azure Key Vault secret management service with support for managed identity and service principal authentication",
            supportedSecretStores: ["AzureKeyVault"],
            supportedSecretTypes: ["Password", "ConnectionString", "ApiKey", "Certificate", "Key"],
            supportsRotation: true,
            supportsVersioning: true,
            supportsSoftDelete: true,
            supportsAccessPolicies: true,
            maxSecretSizeBytes: 25600, // Azure Key Vault limit is 25 KB
            defaultContainerName: "AzureKeyVaultSecretManagerConfiguration")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<ISecretManagerProvider>();

            // Resolve factory from DI (registered in Phase 1)

            // Register factory instance with provider
            var factoryResult = provider.Register(Name, () => services.GetRequiredService<IAzureKeyVaultSecretManagerProvider>());
            if (!factoryResult.IsSuccess) return factoryResult.ToNewResult<IHost>();



            // Why: Typed body providers are registered with the header provider (SecretManagerConfigurationProvider)
            // via discriminator dispatch. AzureKeyVaultConfiguration no longer inherits
            // SecretManagerConfiguration — it implements ISecretManagerImplementationConfiguration directly.
            services.GetRequiredService<ISecretManagerConfigurationProvider>().Register(Name, services.GetRequiredService<IAzureKeyVaultSecretManagerConfigurationProvider>());
    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {

            // No special infrastructure dependencies needed for Azure Key Vault
            // The Azure SDK handles HTTP client internally

            // Register the factory as singleton - DI handles all constructor dependencies
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IAzureKeyVaultSecretManagerFactory), typeof(AzureKeyVaultSecretManagerFactory), ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IAzureKeyVaultSecretManagerProvider), typeof(AzureKeyVaultSecretManagerProvider), ServiceLifetime.Singleton));
            builder.Services.AddSingleton<IAzureKeyVaultSecretManagerConfigurationProvider, AzureKeyVaultSecretManagerConfigurationProvider>(sp => new AzureKeyVaultSecretManagerConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<AzureKeyVaultSecretManagerConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                SecretManagerTypes.ConfigurationConnection, PathName, Container));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

    }



}