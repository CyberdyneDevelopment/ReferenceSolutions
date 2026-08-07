using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Hosting.Abstractions.Configuration;
using Fdw.Services.Authorization;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using ReferenceMultitenancy.Sql.Configuration;
using ReferenceMultitenancy.Sql.Logging;
using ReferenceMultitenancy.Sql.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceMultitenancy.Sql;
using Fdw.Services.Multitenancy.Sql.Extensions;
using Fdw.Services.Multitenancy.Sql.Middleware;
using Fdw.Services.Multitenancy.Sql.Models;
using Fdw.Services.Multitenancy.Sql.Results;
using Fdw.Services.Multitenancy.Sql;
using Fdw.Services.Multitenancy.Sql.Logging;
using Fdw.Services.Multitenancy;
using Fdw.Services;
using Fdw;

namespace ReferenceMultitenancy.Sql;

/// <summary>
/// Multitenancy option for hosts backed by a real, SQL-resolved tenant store: tenants, the
/// tenant-aware connection name provider, organizations, and org-tier access grants.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(MultitenancyTypes), "Sql")]
public sealed class SqlMultitenancyType : MultitenancyTypeBase<ISqlMultitenancyFactory>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlMultitenancyType"/> class.
    /// </summary>
    public SqlMultitenancyType() : base(
        name: "Sql",
        sectionName: "TenantProviders",
        displayName: "SQL Multi-Tenancy",
        description: "Real tenant store: SQL-resolved tenants, organizations, and org-tier access grants")
    {
        // Why: forces the SqlTenantConfiguration singleton so the settings.SqlTenantProvider
        // gateway read (and its loud failure on a missing row) happens in the post-Build
        // fail-fast phase, not lazily on the first request.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
        _ = services.GetRequiredService<SqlTenantConfiguration>();
        return host;
        });

        // Why no Configuration phase: this option used to bind a `Multitenancy` IConfiguration section
        // into List<MultitenancyConfiguration> so a host could declare ServiceOptionType via
        // appsettings or Multitenancy__0__ServiceOptionType. Nothing ever read that value — the active
        // option is selected from ConfigurationSchema.Multitenancy (configurationSchema.json) — so the
        // binding was a lever that looked live and was not. Removed rather than left in place.

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {

            RegisterAlwaysOnContexts(builder.Services);

            // Why: settings.SqlTenantProvider is read through the standard gateway-backed configuration
            // provider (mirrors SettingsConfigurationProvider.RegisterDomainConfiguration) rather than
            // IConfiguration — the row is shared ConfigurationDb data, not per-host appsettings.
            builder.Services.TryAddSingleton<DefaultConfigurationProvider<SqlTenantConfiguration, SqlTenantProviderConfigurationCommand>>(sp =>
                new DefaultConfigurationProvider<SqlTenantConfiguration, SqlTenantProviderConfigurationCommand>(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultConfigurationProvider<SqlTenantConfiguration, SqlTenantProviderConfigurationCommand>>()
                        ?? NullLogger<DefaultConfigurationProvider<SqlTenantConfiguration, SqlTenantProviderConfigurationCommand>>.Instance,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    "ConfigurationDb",
                    "settings",
                    new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

            // Why: deferred singleton factory — reads settings.SqlTenantProvider's single row on first
            // resolution. Not resolved here (Phase 1b runs before Build(), IConfigurationGateway is not yet
            // available); Initialize forces resolution in the post-Build fail-fast phase so a missing row
            // surfaces at startup, matching the sanctioned sync-over-async seam used by
            // ConfigurationGatewayDataStoreProvider.Initialize / OpenIddictSigningKeyConfigurator.
            builder.Services.TryAddSingleton(ResolveTenantConfiguration);

            builder.Services.AddScoped<ITenantProvider, SqlTenantProvider>();
            builder.Services.AddScoped<IConfigurationConnectionNameProvider, TenantAwareConfigurationConnectionNameProvider>();

            // Why: IOrganizationProvider reads tenant.Organizations for org resolution and default-org
            // lookup. Registered here (not in Authorization) because it belongs to the multitenancy
            // domain and requires SqlTenantConfiguration for DataStore/path names.
            // Why: Scoped — it holds the scoped IDataGateway, and its consumer DefaultPrincipalResolver
            // is itself scoped (ProcessSignInClaimsHandler runs scoped). Token issuance resolves it
            // from the request scope, so org_id is populated; a singleton here would be a captive
            // dependency on the scoped gateway.
            builder.Services.TryAddScoped<IOrganizationProvider>(sp =>
                new SqlOrganizationProvider(
                    sp.GetRequiredService<IDataGateway>(),
                    sp.GetRequiredService<SqlTenantConfiguration>(),
                    sp.GetService<ILogger<SqlOrganizationProvider>>()));

            // Why: TenantOrgAccessConfigurationProvider is the domain-owned gateway path.
            // Singleton — holds no per-request state; the IConfigurationGateway is also singleton.
            builder.Services.TryAddSingleton<TenantOrgAccessConfigurationProvider>(sp =>
                new TenantOrgAccessConfigurationProvider(
                    sp.GetRequiredService<IConfigurationGateway>(),
                    sp.GetService<ILogger<TenantOrgAccessConfigurationProvider>>()));

            // Why: IOrgAccessProvider reads tenant.TenantOrgAccess for the org-tier grants used by
            // DefaultAuthorizationService and EffectivePermissionResolver. Scoped — resolves via
            // Lazy<IOrgAccessProvider> at first call within the request.
            builder.Services.TryAddScoped<IOrgAccessProvider>(sp =>
                new DefaultOrgAccessProvider(
                    sp.GetRequiredService<TenantOrgAccessConfigurationProvider>(),
                    sp.GetService<ILogger<DefaultOrgAccessProvider>>()));

            return builder;
    
        });

    }

    /// <inheritdoc/>
    public override bool EnablesTenantResolution => true;

    /// <summary>
    /// The <c>settings.SqlTenantProvider</c> row is NOT read during Configure/Register — Phase 1 runs before the
    /// ServiceProvider is built, so <c>IConfigurationGateway</c> is not yet resolvable. It is instead
    /// read through the gateway-backed <see cref="SqlTenantConfiguration"/> singleton registered in
    /// the Register phase and forced during the Initialize phase (the post-Build
    /// fail-fast phase) — a missing row fails loud there, not on the first request.
    /// </summary>

    /// <summary>Phase 2: register the always-on contexts plus the SQL-backed tenant/org/org-access providers.</summary>


    // Why: extracted from the Registration phase body — the DefaultConfigurationProvider.Get() read is
    // blocked-on synchronously exactly once (forced by Initialize, the post-Build fail-fast phase; see
    // ConfigurationGatewayDataStoreProvider.Initialize for the same sanctioned sync-over-async seam).
    // A missing/empty settings.SqlTenantProvider result is a startup misconfiguration — fail loud
    // rather than construct an empty/default SqlTenantConfiguration (NO FALLBACKS).
    private static SqlTenantConfiguration ResolveTenantConfiguration(IServiceProvider sp)
    {
        var provider = sp.GetRequiredService<DefaultConfigurationProvider<SqlTenantConfiguration, SqlTenantProviderConfigurationCommand>>();
        var logger = sp.GetService<ILoggerFactory>()?.CreateLogger("SqlMultitenancyType") ?? NullLogger.Instance;

#pragma warning disable VSTHRD002
        var result = provider.Get().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002

        // Why these are two checks and not one: a failed read and an empty table are different faults
        // with different fixes. Collapsing them meant a connection/permission/undeclared-container
        // failure reported "has no current row", sending the operator to add a row that already
        // existed. The DI factory signature (Func<IServiceProvider, SqlTenantConfiguration>) cannot
        // return a result, so both still throw — but each carries its own MessageLogging line.
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                SqlMultitenancyLog.TenantProviderReadFailed(
                    logger, result.CurrentMessage ?? string.Empty).Message);
        }

        if (result.Value is null || result.Value.Count == 0)
        {
            throw new InvalidOperationException(
                SqlMultitenancyLog.TenantProviderRowMissing(logger).Message);
        }

        return result.Value[0];
    }
}
