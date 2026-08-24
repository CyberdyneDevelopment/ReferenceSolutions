using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Security.Hashing;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Configuration;
using Fdw.Services.Credentials;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Credentials.Sql.Configuration;
using Fdw.Services.Credentials.Sql.Options;
using ReferenceCredentials.Sql.Services;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.DataVault.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Fdw.Services.Credentials.Logging;
using Fdw.Services.Credentials.Sql.Outcomes;
using Fdw.Services.DataVault;
using Fdw.Services.DataVault.Logging;
using Fdw.Services;
using Fdw;
using Fdw.Results;

namespace ReferenceCredentials.Sql.Registration;

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
[ServiceTypeOption(typeof(CredentialServiceTypes), "Sql")]
public sealed class SqlCredentialServiceType
    : CredentialServiceTypeBase<ICredentialService, ICredentialServiceFactory<ICredentialService, CredentialServiceConfiguration>, CredentialServiceConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlCredentialServiceType"/> class.
    /// </summary>
    public SqlCredentialServiceType() : base(
        name: "Sql",
        sectionName: "Sql",
        displayName: "SQL Credential Service",
        description: "SQL credential service backed by a credential vault, with PAT and agent key services")
    {
        // Why: dataStoreName/pathName flow from the collection sweep so location strings are
        // never hardcoded in the provider construction.
        Registration((builder, loggerFactory) =>
        {

            // Why: SqlCredentialServiceFactory holds the SCOPED IDataVaultProvider, so the factory is
            // Scoped — a Singleton holding a Scoped dependency is the captive-lifetime bug. The scoped
            // CredentialServiceProvider wires this scoped factory per request scope in RegisterFactory.
            builder.Services.TryAddScoped<SqlCredentialServiceFactory>(sp =>
                new SqlCredentialServiceFactory(
                    sp.GetRequiredService<IDataVaultProvider>(),
                    sp.GetService<ILoggerFactory>()));

            // Why: ICredentialServiceFactory<ICredentialService, CredentialServiceConfiguration> is the
            // interface the ServiceTypeCollection machinery uses to dispatch credential service creation.
            builder.Services.TryAddScoped<ICredentialServiceFactory<ICredentialService, CredentialServiceConfiguration>>(
                sp => sp.GetRequiredService<SqlCredentialServiceFactory>());

            // Why: typed body provider reads sec.SqlCredentialService rows keyed by CredentialServiceId.
            builder.Services.TryAddSingleton<DefaultConfigurationProvider<SqlCredentialServiceConfiguration, SqlCredentialServiceConfigurationCommand>>(sp =>
                new DefaultConfigurationProvider<SqlCredentialServiceConfiguration, SqlCredentialServiceConfigurationCommand>(
                    sp.GetService<ILogger<DefaultConfigurationProvider<SqlCredentialServiceConfiguration, SqlCredentialServiceConfigurationCommand>>>(),
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    DataStore,
                    PathName,
                    new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

            builder.Services.TryAddSingleton<IServiceConfigurationProvider<SqlCredentialServiceConfiguration>>(
                sp => sp.GetRequiredService<DefaultConfigurationProvider<SqlCredentialServiceConfiguration, SqlCredentialServiceConfigurationCommand>>());

            // Why: the stateless token/key generator mints raw values + extracts display prefixes for the
            // PAT and agent-key builder.Services. Peppering and verification now happen INSIDE the credential vault
            // (its single pepper), so the separate password/PAT hashers are gone.
            builder.Services.TryAddSingleton<IPersonalAccessTokenGenerator, PersonalAccessTokenGenerator>();

            // Why: Scoped to match the DataVault/connection lifetimes — never Singleton (the
            // captive-lifetime class of bug that broke login).
            builder.Services.TryAddScoped<IPersonalAccessTokenService, SqlPersonalAccessTokenService>();
            builder.Services.TryAddScoped<IAgentKeyService, SqlAgentKeyService>();

            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

        // Why Initialize: this wiring needs a LIVE container, and Register runs while the
        // container is still being built.
        Initialization((host, hostLoggerFactory) =>
        {
            var services = host.Services;
            var provider = (CredentialServiceProvider)services.GetRequiredService<ICredentialServiceProvider>();

            var factory = services.GetRequiredService<SqlCredentialServiceFactory>();

            var factoryRegResult = provider.Register(Name, factory);
            if (!factoryRegResult.IsSuccess)
            {
                // Why: fail loud — silently returning leaves the credential domain with no factory and
                // surfaces later as an opaque "No factory registered" with no clue why. Log the real
                // reason and throw so the scoped-wiring try/catch surfaces it instead of going dark.
                var logger = services.GetService<ILoggerFactory>()?.CreateLogger<SqlCredentialServiceType>();
                if (logger != null)
                    Fdw.ServiceTypes.Logging.ServiceTypeLog.FactoryRegistrationFailed(
                        logger, Name, factoryRegResult.CurrentMessage ?? "provider.Register returned failure");
                throw new InvalidOperationException(
                    $"Failed to register credential service factory '{Name}': {factoryRegResult.CurrentMessage ?? "unknown"}");
            }

            // Why: Typed body providers are registered with the header provider
            // (CredentialServiceConfigurationProvider) via discriminator dispatch, exactly mirroring how
            // CredentialVaultType registers with DataVaultConfigurationProvider. The typed override
            // bridges the invariant IServiceConfigurationProvider<SqlCredentialServiceConfiguration> into
            // the dict that holds IServiceConfigurationProvider<ICredentialServiceConfiguration>.
            var headerProvider = services.GetRequiredService<CredentialServiceConfigurationProvider>();
            var configProvider = services.GetRequiredService<DefaultConfigurationProvider<SqlCredentialServiceConfiguration, SqlCredentialServiceConfigurationCommand>>();
            headerProvider.Register(Name, configProvider);
    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

            // Why: Bind IOptionsMonitor<List<SqlCredentialServiceConfiguration>> from "CredentialServices:Sql"
            // so the typed body provider can pick up the typed builder.Configuration rows loaded by the
            // builder.Configuration source.
            builder.Services.AddOptions<List<SqlCredentialServiceConfiguration>>()
                .BindConfiguration("CredentialServices:Sql");

            // Why: CredentialsSqlOptions carries the SELECTOR ONLY (which credential service this app
            // uses) — no policy. Same species as Users:CredentialServiceName. A missing/blank name fails
            // loud at first credential operation (not at startup).
            builder.Services.Configure<CredentialsSqlOptions>(
                builder.Configuration.GetSection(CredentialsSqlOptions.SectionName));
    
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }


}
