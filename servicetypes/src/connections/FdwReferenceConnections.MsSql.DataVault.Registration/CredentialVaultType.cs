using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.ServiceTypes.Logging;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.DataVault;
using Fdw.Services.DataVault.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace FdwReferenceConnections.MsSql.DataVault.Registration;

/// <summary>
/// Service type definition for the SQL Server credential data vault, registered as the "Default"
/// <c>[Implementation]</c> on <c>DataVaultTypes</c>. There is exactly one vault option in the
/// framework — this one — and its concrete implementation is <see cref="CredentialVault"/>. Third parties
/// add vault implementations by registering their own <c>[Implementation]</c> against
/// <c>DataVaultTypes</c>.
/// </summary>
/// <remarks>
/// Three-phase registration: bind IOptions from "DataVaults:Default", register the factory (which
/// resolves its own connection + secret-manager providers) and the typed-body config provider backed
/// by <c>sec.DefaultDataVault</c>, then wire the factory into the domain provider and register the
/// typed provider with the header provider.
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(DataVaultTypes), "Default")]
public sealed class CredentialVaultType
    : DataVaultTypeBase<IDataVault, ICredentialVaultFactory, SqlDataVaultConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialVaultType"/> class.
    /// </summary>
    public CredentialVaultType() : base(
        name: "Default",
        sectionName: "Default",
        displayName: "Default Data Vault",
        description: "SQL Server credential vault for password, personal-access-token, and agent-key secrets",
        category: "DataVault", defaultContainerName: "DefaultDataVaultConfiguration")
    {
        Configuration(builder =>
        {

            // Why: Bind IOptionsMonitor<List<SqlDataVaultConfiguration>> from "DataVaults:Default"
            // so the typed body provider can pick up static appsettings declarations. The section name
            // matches the [ManagedConfiguration(ServiceType = "Default")] on SqlDataVaultConfiguration.
            builder.Services.AddOptions<List<SqlDataVaultConfiguration>>()
                .BindConfiguration("DataVaults:Default");
    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {

            // Why: the factory resolves its own connection + secret-manager providers, exactly as
            // MsSqlConnectionFactory takes ISecretManagerProvider as its own constructor dependency. The
            // connection is a declared one: the vault's store is the host's, never a ConfigurationDb row (FDW-1084).
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(ICredentialVaultFactory),
                sp => new CredentialVaultFactory(
                    sp.GetRequiredService<IDeclaredConnectionProvider>(),
                    sp.GetRequiredService<ISecretManagerProvider>(),
                    sp.GetRequiredService<IAuthenticationContextAccessor>(),
                    sp.GetService<ILoggerFactory>()),
                ServiceLifetime.Singleton));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(ICredentialVaultProvider),
                sp => new CredentialVaultProvider(sp.GetRequiredService<ICredentialVaultFactory>()),
                ServiceLifetime.Singleton));

            // The factory takes its own SqlDataVaultConfiguration record; DataVaultProvider hands it the
            // record its domain dispatched to, and the factory's base rejects any other type.

            // The domain and implementation config provider reads sec.DefaultDataVault rows keyed by
            // DataVaultId. SqlDataVaultConfigurationCommand targets "DefaultDataVault" — the surviving
            // table name after the CredentialDataVault duplicate was removed.
            builder.Services.AddSingleton<IDefaultDataVaultConfigurationProvider, DefaultDataVaultConfigurationProvider>(sp => new DefaultDataVaultConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<DefaultDataVaultConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DataVaultTypes.ConfigurationConnection, PathName, Container));

            // Decorate the domain's own AddScoped<IDataVaultProvider> registration -- already in
            // builder.Services by the time this runs, since DataVaultTypes' own Registration body
            // sets it up before running the option loop.
            var existing = builder.Services.Single(d => d.ServiceType == typeof(IDataVaultProvider));
            builder.Services.Remove(existing);
            builder.Services.AddScoped<IDataVaultProvider>(sp =>
            {
                var provider = (IDataVaultProvider)existing.ImplementationFactory!(sp);

                var factoryResult = provider.Register(Name, () => sp.GetRequiredService<ICredentialVaultProvider>());
                if (!factoryResult.IsSuccess)
                {
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        sp.GetService<ILoggerFactory>()?.CreateLogger<CredentialVaultType>() ?? NullLogger<CredentialVaultType>.Instance,
                        nameof(CredentialVaultType), Name, nameof(ICredentialVaultProvider), factoryResult.CurrentMessage);
                }

                return provider;
            });

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // IDataVaultConfigurationProvider is Singleton, so root's Initialize call reaches the same
        // instance every later resolution sees -- unlike the domain SERVICE provider above, this one
        // is safe to populate once, here, unmodified.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            services.GetRequiredService<IDataVaultConfigurationProvider>()
                .Register(Name, services.GetRequiredService<IDefaultDataVaultConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });
    }
}
