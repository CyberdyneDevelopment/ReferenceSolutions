using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.DataVault;
using Fdw.Services.DataVault.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ReferenceCredentials.Sql;
using ReferenceConnections.MsSql.DataVault;
using Fdw.Results;

namespace ReferenceConnections.MsSql.DataVault.Registration;

/// <summary>
/// Service type definition for the SQL Server credential data vault, registered as the "Default"
/// <c>[ServiceTypeOption]</c> on <c>DataVaultServiceTypes</c>. There is exactly one vault option in the
/// framework — this one — and its concrete implementation is <see cref="CredentialVault"/>. Third parties
/// add vault implementations by registering their own <c>[ServiceTypeOption]</c> against
/// <c>DataVaultServiceTypes</c>.
/// </summary>
/// <remarks>
/// Three-phase registration: bind IOptions from "DataVaults:Default", register the PURE factory and the
/// typed-body config provider backed by <c>sec.DefaultDataVault</c>, then wire the factory into the domain
/// provider, call <c>ConfigureResolution</c>, and register the typed provider with the header provider.
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(DataVaultServiceTypes), "Default")]
public sealed class CredentialVaultType
    : DataVaultTypeBase<IDataVault, IDataVaultFactory<IDataVault, DataVaultConfiguration>, DataVaultConfiguration>
{
    private IServiceConfigurationProvider<DefaultDataVaultConfiguration>? _typedBodyProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialVaultType"/> class.
    /// </summary>
    public CredentialVaultType() : base(
        name: "Default",
        sectionName: "Default",
        displayName: "Default Data Vault",
        description: "SQL Server credential vault for password, personal-access-token, and agent-key secrets",
        category: "DataVault")
    {
        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = (DefaultDataVaultProvider)services.GetRequiredService<IDataVaultProvider>();

            var factory = services.GetRequiredService<CredentialVaultFactory>();

            var factoryRegResult = provider.Register(Name, factory);
            if (!factoryRegResult.IsSuccess)
            {
                // Why: fail loud — silently returning leaves the data-vault domain with no factory and
                // surfaces later as an opaque "No factory registered" with no clue why. Log the real
                // reason and throw so the scoped-wiring try/catch surfaces it instead of going dark.
                var logger = services.GetService<ILoggerFactory>()?.CreateLogger<CredentialVaultType>();
                if (logger != null)
                    Fdw.ServiceTypes.Logging.ServiceTypeLog.FactoryRegistrationFailed(
                        logger, Name, factoryRegResult.CurrentMessage ?? "provider.Register returned failure");
                throw new InvalidOperationException(
                    $"Failed to register data vault factory '{Name}': {factoryRegResult.CurrentMessage ?? "unknown"}");
            }

            // Why: the ServiceTypeCollection generator constructs DefaultDataVaultProvider with logger
            // ONLY (new DefaultDataVaultProvider(providerLogger)), so the resolution providers cannot be
            // ctor-injected. Wire them here in phase 3 — where the built container is available. Idempotent:
            // each vault type sets the same connection + secret-manager providers.
            provider.ConfigureResolution(
                services.GetRequiredService<IDataConnectionProvider>(),
                services.GetRequiredService<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>());

            // Why: Typed body providers are registered with the header provider (DataVaultConfigurationProvider)
            // via discriminator dispatch, exactly mirroring how connection types register with
            // ConnectionConfigurationProvider.
            var headerProvider = services.GetRequiredService<DataVaultConfigurationProvider>();
            var configProvider = services.GetRequiredService<IServiceConfigurationProvider<DefaultDataVaultConfiguration>>();
            headerProvider.Register(Name, configProvider);

            // Why: store typed provider reference so typed body resolution in future admin UI paths
            // can delegate without re-resolving from DI on hot paths.
            _typedBodyProvider = configProvider;
    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

            // Why: Bind IOptionsMonitor<List<DefaultDataVaultConfiguration>> from "DataVaults:Default"
            // so the typed body provider can pick up static appsettings declarations. The section name
            // matches the [ManagedConfiguration(ServiceType = "Default")] on DefaultDataVaultConfiguration.
            builder.Services.AddOptions<List<DefaultDataVaultConfiguration>>()
                .BindConfiguration("DataVaults:Default");
    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {

            // Why: the factory is a PURE constructor — it takes no providers. The connection + pepper are
            // resolved by DefaultDataVaultProvider (once per vault) and passed to factory.Create.
            builder.Services.TryAddSingleton<CredentialVaultFactory>(sp =>
                new CredentialVaultFactory(sp.GetService<ILoggerFactory>()));

            // Why: IDataVaultFactory<IDataVault, DataVaultConfiguration> is the interface the
            // ServiceTypeCollection machinery uses to dispatch vault creation.
            builder.Services.TryAddSingleton<IDataVaultFactory<IDataVault, DataVaultConfiguration>>(
                sp => sp.GetRequiredService<CredentialVaultFactory>());

            // Why: typed body provider reads sec.DefaultDataVault rows keyed by DataVaultId.
            // DefaultDataVaultConfigurationCommand targets "DefaultDataVault" — the surviving table name
            // after the CredentialDataVault duplicate was removed.
            builder.Services.TryAddSingleton<DefaultConfigurationProvider<DefaultDataVaultConfiguration, DefaultDataVaultConfigurationCommand>>(sp =>
                new DefaultConfigurationProvider<DefaultDataVaultConfiguration, DefaultDataVaultConfigurationCommand>(
                    sp.GetService<ILogger<DefaultConfigurationProvider<DefaultDataVaultConfiguration, DefaultDataVaultConfigurationCommand>>>(),
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    DataStore,
                    PathName,
                    new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

            builder.Services.TryAddSingleton<IServiceConfigurationProvider<DefaultDataVaultConfiguration>>(
                sp => sp.GetRequiredService<DefaultConfigurationProvider<DefaultDataVaultConfiguration, DefaultDataVaultConfigurationCommand>>());

            // Why: RegisterFactory (below) requires DataVaultConfigurationProvider (the shared header
            // provider for the whole DataVault domain) to already be registered. TryAddSingleton inside
            // RegisterDomainConfiguration makes this idempotent — harmless if another vault option also calls it.
            DataVaultConfigurationProvider.RegisterDomainConfiguration(builder.Services);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

    }



}
