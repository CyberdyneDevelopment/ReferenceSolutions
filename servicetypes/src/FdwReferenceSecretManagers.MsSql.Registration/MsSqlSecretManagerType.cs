using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.Collections;
using Fdw.Security.Hashing;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using FdwReferenceSecretManagers.MsSql.Configuration;
using Fdw.Results;

namespace FdwReferenceSecretManagers.MsSql.Registration;

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
[Implementation(typeof(SecretManagerTypes), "MsSql")]
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
            defaultContainerName: "MsSqlSecretManagerConfiguration")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<ISecretManagerProvider>();

            // Resolve factory from DI (registered in Phase 1)

            // Register factory instance with provider
            var factoryResult = provider.Register(Name, () => services.GetRequiredService<IMsSqlSecretManagerProvider>());

            // Why: Typed body providers are registered with the header provider (SecretManagerConfigurationProvider)
            // via discriminator dispatch. MsSqlSecretManagerConfiguration no longer inherits
            // SecretManagerConfiguration — it implements ISecretManagerImplementationConfiguration directly.
            // This registration is independent of the factory registration above and must happen either
            // way: without it the header composes with no typed body, and the failure surfaces much later
            // as "No implementation configuration provider registered for Implementation 'MsSql'".
            var headerResult = services.GetRequiredService<ISecretManagerConfigurationProvider>().Register(Name, services.GetRequiredService<IMsSqlSecretManagerConfigurationProvider>());

            if (!factoryResult.IsSuccess) return factoryResult.ToNewResult<IHost>();
            if (!headerResult.IsSuccess) return headerResult.ToNewResult<IHost>();

            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {

            // Why: the factory reads/writes through IConfigurationGateway (PlatformConfiguration) rather than a
            // hand-built IDataConnection — DataStore flows from TypeCollection.Configure() so
            // the connection is never hardcoded here, mirroring the ImplementationConfigurationProviderBase registration below.
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IMsSqlSecretManagerFactory),
                sp => new MsSqlSecretManagerFactory(
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DataStore,
                sp.GetService<IPasswordHasher>(),
                sp.GetService<IPersonalAccessTokenHasher>(),
                sp.GetService<IPersonalAccessTokenGenerator>(),
                null),
                ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IMsSqlSecretManagerProvider),
                sp => new MsSqlSecretManagerProvider(sp.GetRequiredService<IMsSqlSecretManagerFactory>()),
                ServiceLifetime.Singleton));

            builder.Services.AddSingleton<IMsSqlSecretManagerConfigurationProvider, MsSqlSecretManagerConfigurationProvider>(sp => new MsSqlSecretManagerConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<MsSqlSecretManagerConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                SecretManagerTypes.ConfigurationConnection, PathName, Container));

            // Why: RegisterFactory (below) requires SecretManagerConfigurationProvider (the shared header
            // provider for the whole SecretManager domain) to already be registered. TryAddSingleton inside
            // RegisterDomainConfiguration makes this idempotent — every secret manager option calls it, first
            // registration wins.
            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

    }



}