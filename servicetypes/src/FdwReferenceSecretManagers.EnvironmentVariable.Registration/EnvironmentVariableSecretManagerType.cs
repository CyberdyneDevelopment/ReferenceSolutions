using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.Collections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using FdwReferenceSecretManagers.EnvironmentVariable.Configuration;
using Fdw.Results;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Registration;

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
[Implementation(typeof(SecretManagerTypes), "EnvironmentVariable")]
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
            defaultContainerName: "EnvironmentVariableSecretManagerConfiguration")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<ISecretManagerProvider>();

            // Resolve factory from DI (registered in Phase 1)

            // Register factory instance with provider
            var factoryResult = provider.Register(Name, () => services.GetRequiredService<IEnvironmentVariableSecretManagerProvider>());
            if (!factoryResult.IsSuccess) return factoryResult.ToNewResult<IHost>();

            // Why: Typed body providers are registered with the header provider (SecretManagerConfigurationProvider)
            // via discriminator dispatch. EnvironmentVariableConfiguration no longer inherits
            // SecretManagerConfiguration — it implements ISecretManagerImplementationConfiguration directly.
            // Register takes the ERASED IServiceConfigurationProvider; TDerived is CAPTURED, not forwarded —
            // this domain's overload also records the CLR type and a reflection-free ctor for Save's default
            // body. The widening is an explicit interface implementation on ImplementationConfigurationProviderBase.
            services.GetRequiredService<ISecretManagerConfigurationProvider>().Register(Name, services.GetRequiredService<IEnvironmentVariableSecretManagerConfigurationProvider>());
    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {

            // No special infrastructure dependencies needed for Environment Variable
            // It uses Environment.GetEnvironmentVariable directly

            // Register the factory as singleton - DI handles all constructor dependencies
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IEnvironmentVariableSecretManagerFactory), typeof(EnvironmentVariableSecretManagerFactory), ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IEnvironmentVariableSecretManagerProvider), typeof(EnvironmentVariableSecretManagerProvider), ServiceLifetime.Singleton));

            builder.Services.AddSingleton<IEnvironmentVariableSecretManagerConfigurationProvider, EnvironmentVariableSecretManagerConfigurationProvider>(sp => new EnvironmentVariableSecretManagerConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<EnvironmentVariableSecretManagerConfigurationProvider>(),
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