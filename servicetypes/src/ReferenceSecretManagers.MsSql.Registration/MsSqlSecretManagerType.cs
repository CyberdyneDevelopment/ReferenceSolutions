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
using Fdw.Security.Hashing;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.MsSql.Commands;
using Fdw.Services.SecretManagers.MsSql.Configuration;
using Fdw.Services;
using Fdw;
using Fdw.Results;

namespace ReferenceSecretManagers.MsSql.Registration;

/// <summary>
/// Service type definition for MsSql secret manager.
/// Handles two-phase registration lifecycle for SQL Server-based secret storage.
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
/// Configuration is loaded from "SecretManagers:MsSql" sections in appsettings.json
/// or from the sec.MsSqlSecretManager table in the configuration database.
/// </para>
/// </remarks>
[ServiceTypeOption(typeof(SecretManagerTypes), "MsSql")]
public sealed class MsSqlSecretManagerType
    : SecretManagerTypeBase<ISecretManager, IMsSqlSecretManagerFactory, MsSqlSecretManagerConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlSecretManagerType"/> class.
    /// </summary>
    public MsSqlSecretManagerType()
        : base(
            name: "MsSql",
            sectionName: "MsSql",
            displayName: "SQL Server",
            description: "Secret manager that stores and retrieves secrets from a SQL Server database table with version-on-write support",
            supportedSecretStores: ["MsSql"],
            supportedSecretTypes: ["Password", "ConnectionString", "ApiKey", "Token"],
            supportsRotation: false,
            supportsVersioning: true,
            supportsSoftDelete: true,
            supportsAccessPolicies: false,
            maxSecretSizeBytes: 2_000_000_000, // NVARCHAR(MAX) limit
            supportsBatchOperations: true,
            supportsExpiration: true,
            defaultContainerName: "MsSqlSecretManager")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>();

            // Resolve factory from DI (registered in Phase 1)
            var factory = services.GetRequiredService<IMsSqlSecretManagerFactory>();

            // Register factory instance with provider
            var factoryResult = provider.Register(Name, factory);
            if (!factoryResult.IsSuccess)
            {
                return GenericResult<IHost>.Success(host);
            }

            // Why: Typed body providers are registered with the header provider (SecretManagerConfigurationProvider)
            // via discriminator dispatch. MsSqlSecretManagerConfiguration no longer inherits
            // SecretManagerConfiguration — it implements ISecretManagerConfiguration directly.
            var headerProvider = services.GetRequiredService<SecretManagerConfigurationProvider>();
            var configProvider = services.GetRequiredService<DefaultConfigurationProvider<MsSqlSecretManagerConfiguration, MsSqlSecretManagerConfigurationCommand>>();
            headerProvider.Register<MsSqlSecretManagerConfiguration>(Name, configProvider);
    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {

            // Why: the factory reads/writes through IConfigurationGateway (ConfigurationDb) rather than a
            // hand-built IDataConnection — DataStore flows from TypeCollection.Configure() so
            // "ConfigurationDb" is never hardcoded here, mirroring the DefaultConfigurationProvider registration below.
            builder.Services.AddSingleton<IMsSqlSecretManagerFactory>(sp => new MsSqlSecretManagerFactory(
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                DataStore,
                sp.GetService<IPasswordHasher>(),
                sp.GetService<IPersonalAccessTokenHasher>(),
                sp.GetService<IPersonalAccessTokenGenerator>(),
                null));

            // Why: Lazy<IDataGateway> defers cfg resolution until first runtime query, avoiding
            // circular dependency with the DataGateway that hasn't been built yet at registration time.
            // DataStore flows from TypeCollection.Configure() so "ConfigurationDb" is never hardcoded here.
            builder.Services.AddSingleton(sp => new DefaultConfigurationProvider<MsSqlSecretManagerConfiguration, MsSqlSecretManagerConfigurationCommand>(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<DefaultConfigurationProvider<MsSqlSecretManagerConfiguration, MsSqlSecretManagerConfigurationCommand>>(),
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                DataStore,
                PathName,
                new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

            // Why: RegisterFactory (below) requires SecretManagerConfigurationProvider (the shared header
            // provider for the whole SecretManager domain) to already be registered. TryAddSingleton inside
            // RegisterDomainConfiguration makes this idempotent — every secret manager option calls it, first
            // registration wins.
            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

    }



}
