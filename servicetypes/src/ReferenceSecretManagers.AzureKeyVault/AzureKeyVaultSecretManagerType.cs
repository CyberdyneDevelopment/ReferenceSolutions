using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.ServiceTypes;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using ReferenceSecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services;
using Fdw;
using Fdw.Results;

namespace ReferenceSecretManagers.AzureKeyVault;

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
[ServiceTypeOption(typeof(SecretManagerTypes), "AzureKeyVault")]
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
            defaultContainerName: "AzureKeyVaultSecretManager")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>();

            // Resolve factory from DI (registered in Phase 1)
            var factory = services.GetRequiredService<IAzureKeyVaultSecretManagerFactory>();

            // Register factory instance with provider
            var factoryResult = provider.Register(Name, factory);
            if (!factoryResult.IsSuccess) return factoryResult.ToNewResult<IHost>();

            var configLogger = services.GetRequiredService<ILoggerFactory>()
                .CreateLogger<DefaultConfigurationProvider<AzureKeyVaultConfiguration, AzureKeyVaultConfigurationCommand>>();

            var lazyGateway = services.GetRequiredService<Lazy<Fdw.Services.Data.Abstractions.IConfigurationGateway>>();
            var configProvider = new DefaultConfigurationProvider<AzureKeyVaultConfiguration, AzureKeyVaultConfigurationCommand>(
                configLogger,
                lazyGateway,
                "ConfigurationDb",
                "sec",
                new Lazy<Fdw.Services.Data.Abstractions.ICacheInvalidator?>(() => services.GetService<Fdw.Services.Data.Abstractions.ICacheInvalidator>()));

            // Why: Typed body providers are registered with the header provider (SecretManagerConfigurationProvider)
            // via discriminator dispatch. AzureKeyVaultConfiguration no longer inherits
            // SecretManagerConfiguration — it implements ISecretManagerConfiguration directly.
            var headerProvider = services.GetRequiredService<SecretManagerConfigurationProvider>();
            headerProvider.Register<AzureKeyVaultConfiguration>(Name, configProvider);
    
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
            builder.Services.AddSingleton<IAzureKeyVaultSecretManagerFactory, AzureKeyVaultSecretManagerFactory>();

            // Why: RegisterFactory (below) requires SecretManagerConfigurationProvider (the shared header
            // provider for the whole SecretManager domain) to already be registered. TryAddSingleton inside
            // RegisterDomainConfiguration makes this idempotent — every secret manager option calls it, first
            // registration wins.
            SecretManagerConfigurationProvider.RegisterDomainConfiguration(builder.Services);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

    }



}
