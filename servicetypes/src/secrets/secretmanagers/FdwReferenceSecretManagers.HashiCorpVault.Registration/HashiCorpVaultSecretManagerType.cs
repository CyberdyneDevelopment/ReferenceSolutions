using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using FdwReferenceSecretManagers.HashiCorpVault.Configuration;

namespace FdwReferenceSecretManagers.HashiCorpVault.Registration;

/// <summary>
/// Service type definition for the HashiCorp Vault secret manager.
/// Handles the two-phase registration lifecycle for the Vault integration.
/// </summary>
/// <remarks>
/// <para>
/// Phase 1 (Registration) puts the factory, its providers and the dependencies the factory takes in the
/// container: an <see cref="System.Net.Http.HttpClient"/> and a lazy handle on the secret manager domain
/// provider (the Vault factory reads the secret that holds its own credentials through that provider, so
/// it cannot take the provider directly without a cycle). Phase 2 (Initialization) hands the providers to
/// the domain once the container is live.
/// </para>
/// <para>
/// What the secret manager does: it reads a secret somebody stored in the KV engine, and has Vault issue
/// short-lived database credentials from the database engine. It stores nothing itself.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(SecretManagerTypes), "HashiCorpVault")]
public sealed class HashiCorpVaultSecretManagerType
    : SecretManagerTypeBase<ISecretManager, IHashiCorpVaultSecretManagerFactory, HashiCorpVaultConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HashiCorpVaultSecretManagerType"/> class.
    /// </summary>
    public HashiCorpVaultSecretManagerType()
        : base(
            name: "HashiCorpVault",
            sectionName: "HashiCorpVault",
            displayName: "HashiCorp Vault",
            description: "Reads stored secrets from HashiCorp Vault's KV engine, and has Vault issue short-lived database credentials from its database engine",
            supportedSecretStores: ["HashiCorpVault"],
            supportedSecretTypes: ["Password", "ConnectionString", "ApiKey", "Token"],
            supportsRotation: false,       // Vault rotates database credentials itself; this type does not rotate a secret
            supportsVersioning: false,     // The KV engine is read at its current version
            supportsSoftDelete: false,     // This type deletes nothing
            supportsAccessPolicies: false, // Vault policies are managed in Vault
            maxSecretSizeBytes: 524288,    // Vault's default maximum value size is 512 KiB
            defaultContainerName: "HashiCorpVaultSecretManagerConfiguration")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<ISecretManagerProvider>();

            var factoryResult = provider.Register(Name, () => services.GetRequiredService<IHashiCorpVaultSecretManagerProvider>());
            if (!factoryResult.IsSuccess) return factoryResult.ToNewResult<IHost>();

            // Why: typed body providers are registered with the header provider (SecretManagerConfigurationProvider)
            // via discriminator dispatch.
            services.GetRequiredService<ISecretManagerConfigurationProvider>().Register(Name, services.GetRequiredService<IHashiCorpVaultSecretManagerConfigurationProvider>());

            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder => GenericResult<IHostApplicationBuilder>.Success(builder));

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddHttpClient();
            builder.Services.TryAddSingleton(sp => new Lazy<IDomainServiceProvider<ISecretManager, ISecretManagerImplementationConfiguration>>(
                () => sp.GetRequiredService<ISecretManagerProvider>()));

            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IHashiCorpVaultSecretManagerFactory), typeof(HashiCorpVaultSecretManagerFactory), ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IHashiCorpVaultSecretManagerProvider), typeof(HashiCorpVaultSecretManagerProvider), ServiceLifetime.Singleton));
            builder.Services.AddSingleton<IHashiCorpVaultSecretManagerConfigurationProvider, HashiCorpVaultSecretManagerConfigurationProvider>(sp => new HashiCorpVaultSecretManagerConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<HashiCorpVaultSecretManagerConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                SecretManagerTypes.ConfigurationConnection, PathName, Container));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
