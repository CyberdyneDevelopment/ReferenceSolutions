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
using Fdw.Services.SecretManagers.Sqlite.Commands;
using Fdw.Services.SecretManagers.Sqlite.Configuration;
using Fdw.Services;
using Fdw;
using Fdw.Results;

namespace ReferenceSecretManagers.Sqlite.Registration;

/// <summary>
/// Service type definition for the SQLite secret manager.
/// Handles two-phase registration lifecycle for SQLite file-based secret storage.
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
/// Configuration is loaded from "SecretManagers:Sqlite" sections in appsettings.json
/// or from the sec.SqliteSecretManager table in the configuration database.
/// </para>
/// </remarks>
[ServiceTypeOption(typeof(SecretManagerTypes), "Sqlite")]
public sealed class SqliteSecretManagerType
    : SecretManagerTypeBase<ISecretManager, ISqliteSecretManagerFactory, SqliteSecretManagerConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteSecretManagerType"/> class.
    /// </summary>
    public SqliteSecretManagerType()
        : base(
            name: "Sqlite",
            sectionName: "Sqlite",
            displayName: "SQLite",
            description: "Secret manager that stores and retrieves secrets from a SQLite database file with version-on-write support",
            supportedSecretStores: ["Sqlite"],
            supportedSecretTypes: ["Password", "ConnectionString", "ApiKey", "Token"],
            supportsRotation: false,
            supportsVersioning: true,
            supportsSoftDelete: true,
            supportsAccessPolicies: false,
            maxSecretSizeBytes: int.MaxValue, // TEXT column — no practical limit
            supportsBatchOperations: true,
            supportsExpiration: true,
            defaultContainerName: "SqliteSecretManager")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>();

            // Resolve factory from DI (registered in Phase 1)
            var factory = services.GetRequiredService<ISqliteSecretManagerFactory>();

            // Register factory instance with provider
            var factoryResult = provider.Register(Name, factory);
            if (!factoryResult.IsSuccess)
            {
                return GenericResult<IHost>.Success(host);
            }

            // Why: Typed body providers are registered with the header provider (SecretManagerConfigurationProvider)
            // via discriminator dispatch. SqliteSecretManagerConfiguration does not inherit
            // SecretManagerConfiguration — it implements ISecretManagerConfiguration directly.
            var headerProvider = services.GetRequiredService<SecretManagerConfigurationProvider>();
            var configProvider = services.GetRequiredService<DefaultConfigurationProvider<SqliteSecretManagerConfiguration, SqliteSecretManagerConfigurationCommand>>();
            headerProvider.Register<SqliteSecretManagerConfiguration>(Name, configProvider);
    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {

            // Register the factory as singleton - DI handles all constructor dependencies
            builder.Services.AddSingleton<ISqliteSecretManagerFactory, SqliteSecretManagerFactory>();

            // Why: Lazy<IConfigurationGateway> defers cfg resolution until first runtime query, avoiding
            // circular dependency with the DataGateway that hasn't been built yet at registration time.
            // DataStore flows from TypeCollection.Configure() so "ConfigurationDb" is never hardcoded here.
            builder.Services.AddSingleton(sp => new DefaultConfigurationProvider<SqliteSecretManagerConfiguration, SqliteSecretManagerConfigurationCommand>(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<DefaultConfigurationProvider<SqliteSecretManagerConfiguration, SqliteSecretManagerConfigurationCommand>>(),
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
