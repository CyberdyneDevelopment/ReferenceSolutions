using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Security.Hashing;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Credentials;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Credentials.Sql.Configuration;
using FdwReferenceCredentials.Sql.Services;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.DataVault.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.ServiceTypes.Logging;
using Fdw.Results;

using Fdw.Services.Credentials.Sql;
namespace FdwReferenceCredentials.Sql.Registration;

/// <summary>
/// Service type definition for the SQL credential service implementation — the credential domain's
/// default option. Provides credential service metadata, factory creation, the typed body
/// configuration provider, and the vault-backed PAT / agent-key services with their hashing
/// primitives.
/// </summary>
/// <remarks>
/// Cross-assembly option of <see cref="CredentialServiceTypes"/> (declared here, in a SIBLING
/// assembly of the credential domain core). The entry-point app's Registration.SourceGenerators
/// module initializer registers it when this package is referenced — package reference IS the
/// registration intent; the app's existing <c>CredentialServiceTypes.Configure/Register/Initialize</c>
/// calls invoke the phases below. No app-side registration code exists for these services.
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(CredentialServiceTypes), "Sql")]
public sealed class SqlCredentialServiceType
    : CredentialServiceTypeBase<ICredentialService, ISqlCredentialServiceFactory, SqlCredentialServiceConfiguration>
{
    // CredentialsSql is this server's own selector -- which credential service it uses -- and this host
    // keeps it in ServerConfiguration (cred), not PlatformConfiguration.
    private static string ServerConfigurationStore => ServerConnection.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlCredentialServiceType"/> class.
    /// </summary>
    public SqlCredentialServiceType() : base(
        name: "Sql",
        sectionName: "Sql",
        displayName: "SQL Credential Service",
        description: "SQL credential service backed by a credential vault, with PAT and agent key services", defaultContainerName: "SqlCredentialServiceConfiguration")
    {
        // Why: dataStoreName/pathName flow from the collection pass so location strings are
        // never hardcoded in the provider construction.
        Registration((builder, loggerFactory) =>
        {

            // Why: SqlCredentialServiceFactory holds the SCOPED IDataVaultProvider, so the factory is
            // Scoped — a Singleton holding a Scoped dependency is the captive-lifetime bug. The scoped
            // CredentialServiceProvider wires this scoped factory per request scope in RegisterFactory.
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(ISqlCredentialServiceFactory),
                sp => new SqlCredentialServiceFactory(
                    sp.GetRequiredService<IDataVaultProvider>(),
                    sp.GetService<ILoggerFactory>()),
                ServiceLifetime.Scoped));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(ISqlCredentialServiceProvider),
                sp => new SqlCredentialServiceProvider(sp.GetRequiredService<ISqlCredentialServiceFactory>()),
                ServiceLifetime.Scoped));

            // The factory takes the IMPLEMENTATION contract: the domain provider dispatches to the
            // implementation provider and what comes back is the implementation configuration.

            // Why: typed body provider reads sec.SqlCredentialService rows keyed by CredentialServiceId.
            builder.Services.AddSingleton<ISqlCredentialServiceConfigurationProvider, SqlCredentialServiceConfigurationProvider>(sp => new SqlCredentialServiceConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<SqlCredentialServiceConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                CredentialServiceTypes.ConfigurationConnection, PathName, Container));


            // Why: the stateless token/key generator mints raw values + extracts display prefixes for the
            // PAT and agent-key builder.Services. Peppering and verification now happen INSIDE the credential vault
            // (its single pepper), so the separate password/PAT hashers are gone.
            builder.Services.TryAddSingleton<IPersonalAccessTokenGenerator, PersonalAccessTokenGenerator>();

            // Why: Scoped to match the DataVault/connection lifetimes — never Singleton (the
            // captive-lifetime class of bug that broke login).
            builder.Services.TryAddSingleton<IAuthenticationContextAccessor, AuthenticationContextAccessor>();
            builder.Services.TryAddScoped<IPersonalAccessTokenService, SqlPersonalAccessTokenService>();
            builder.Services.TryAddScoped<IPasswordLinkService, SqlPasswordLinkService>();
            builder.Services.TryAddScoped<IAgentKeyService, SqlAgentKeyService>();

            // Decorate the domain's own AddScoped<ICredentialServiceProvider> registration -- already
            // in builder.Services by the time this runs, since CredentialServiceTypes' own
            // Registration body sets it up before running the option loop.
            var existing = builder.Services.Single(d => d.ServiceType == typeof(ICredentialServiceProvider));
            builder.Services.Remove(existing);
            builder.Services.AddScoped<ICredentialServiceProvider>(sp =>
            {
                var provider = (ICredentialServiceProvider)existing.ImplementationFactory!(sp);

                var factoryResult = provider.Register(Name, () => sp.GetRequiredService<ISqlCredentialServiceProvider>());
                if (!factoryResult.IsSuccess)
                {
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        sp.GetService<ILoggerFactory>()?.CreateLogger<SqlCredentialServiceType>() ?? NullLogger<SqlCredentialServiceType>.Instance,
                        nameof(SqlCredentialServiceType), Name, nameof(ISqlCredentialServiceProvider), factoryResult.CurrentMessage);
                }

                return provider;
            });

            return GenericResult<IHostApplicationBuilder>.Success(builder);

        });

        Configuration(builder =>
        {

            // Why: Bind IOptionsMonitor<List<SqlCredentialServiceConfiguration>> from "CredentialServices:Sql"
            // so the typed body provider can pick up the typed builder.Configuration rows loaded by the
            // builder.Configuration source.
            builder.Services.AddOptions<List<SqlCredentialServiceConfiguration>>()
                .BindConfiguration("CredentialServices:Sql");

            // Why: the selector (which credential service this host uses) is the Sql implementation of the
            // credential-store domain, kept in ServerConfiguration: CredentialStore is the domain row and
            // CredentialsSql the implementation joined to it. Registering the implementation into the domain
            // is what lets a read of "CredentialStore" compose it. A missing/blank name still fails loud at
            // first credential operation (not at startup).
            builder.Services.TryAddSingleton(sp => new CredentialsSqlConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<CredentialsSqlConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ServerConfigurationStore, "cred", "CredentialsSqlConfiguration"));
            builder.Services.TryAddSingleton(sp =>
            {
                var domain = new CredentialStoreConfigurationProvider(
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<CredentialStoreConfigurationProvider>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ServerConfigurationStore, "cred", "CredentialStore");
                domain.Register(Name, sp.GetRequiredService<CredentialsSqlConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<ICredentialStoreConfigurationProvider>(sp => sp.GetRequiredService<CredentialStoreConfigurationProvider>());

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // ICredentialServiceConfigurationProvider is Singleton, so root's Initialize call reaches
        // the same instance every later resolution sees -- unlike the domain SERVICE provider
        // above, this one is safe to populate once, here, unmodified.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            services.GetRequiredService<ICredentialServiceConfigurationProvider>()
                .Register(Name, services.GetRequiredService<ISqlCredentialServiceConfigurationProvider>());
            return GenericResult<IHost>.Success(host);
        });
    }
}
