using Fdw.Services.Multitenancy;
using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using FdwReferenceMultitenancy.Sql.Logging;
using FdwReferenceMultitenancy.Sql.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

namespace FdwReferenceMultitenancy.Sql.Registration;

/// <summary>
/// Multitenancy option for hosts backed by a real, SQL-resolved tenant store: tenants,
/// organizations, and org-tier access grants.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(MultitenancyTypes), "Sql")]
public sealed class SqlMultitenancyType : MultitenancyTypeBase<ISqlMultitenancyFactory>
{
    /// <summary>The Multitenancy domain row this option implements, read by the domain's own name.</summary>
    private const string DomainName = "Multitenancy";

    /// <summary>Initializes a new instance of the <see cref="SqlMultitenancyType"/> class.</summary>
    public SqlMultitenancyType() : base(
        name: "Sql",
        sectionName: "TenantProviders",
        displayName: "SQL Multi-Tenancy",
        description: "Real tenant store: SQL-resolved tenants, organizations, and org-tier access grants")
    {
        // Why: forces the SqlTenantConfiguration singleton so the settings.SqlTenantProviderConfiguration
        // gateway read (and its loud failure on a missing row) happens in the post-Build
        // fail-fast phase, not lazily on the first request.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;

            var registered = services.GetRequiredService<IMultitenancyConfigurationProvider>()
                .Register(Name, services.GetRequiredService<ISqlTenantProviderImplementationConfigurationProvider>());
            if (!registered.IsSuccess) return registered.ToNewResult<IHost>();

            _ = services.GetRequiredService<SqlTenantConfiguration>();
            return GenericResult<IHost>.Success(host);
        });

        // Why no Configuration phase: this option used to bind a `Multitenancy` IConfiguration section
        // into List<MultitenancyConfiguration> so a host could declare Implementation via
        // appsettings or Multitenancy__0__Implementation. Nothing ever read that value — the active
        // option is selected from ConfigurationSchema.Multitenancy (configurationSchema.json) — so the
        // binding was a lever that looked live and was not. Removed rather than left in place.

        Registration((builder, loggerFactory) =>
        {

            RegisterAlwaysOnContexts(builder.Services);

            // settings.SqlTenantProviderConfiguration is this domain's Sql implementation, read through the
            // Multitenancy domain provider rather than IConfiguration -- the row is shared platform
            // data, not per-host appsettings.
            builder.Services.AddSingleton<ISqlTenantProviderImplementationConfigurationProvider, SqlTenantProviderImplementationConfigurationProvider>(sp => new SqlTenantProviderImplementationConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<SqlTenantProviderImplementationConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                MultitenancyTypes.ConfigurationConnection, "settings", "SqlTenantProviderConfiguration"));

            // Why: deferred singleton factory — reads settings.SqlTenantProviderConfiguration's single row on first
            // resolution. Not resolved here (Phase 1b runs before Build(), IConfigurationGateway is not yet
            // available); Initialize forces resolution in the post-Build fail-fast phase so a missing row
            // surfaces at startup, matching the sanctioned sync-over-async seam used by
            // ConfigurationGatewayDataStoreProvider.Initialize / OpenIddictSigningKeyConfigurator.
            builder.Services.TryAddSingleton(ResolveTenantConfiguration);

            builder.Services.AddScoped<ITenantProvider, SqlTenantProvider>();

            // Why beside the provider: the writes are the other half of the same tenants table (FDW-1578), and a tenant is read back
            // through ITenantProvider before it is changed.
            builder.Services.AddScoped<ITenantWriter, SqlTenantWriter>();
            builder.Services.AddScoped<IOrganizationWriter, SqlOrganizationWriter>();

            // Why beside the tenant tables: a feature is switched on for a tenant in the table SqlTenantProvider reads its features from
            // (TenantFeaturesTableName), so the switches take the store, path and that table from SqlTenantConfiguration; the table of
            // the features a tenant switched on for its organizations sits beside it (FDW-1580).
            builder.Services.AddScoped<IFeatureEnablement>(sp =>
            {
                var tenant = sp.GetRequiredService<SqlTenantConfiguration>();
                return new FeatureEnablement(
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    new FeatureTables(tenant.DataStoreName!, tenant.PathName!, tenant.TenantFeaturesTableName!, "OrganizationFeatures"),
                    sp.GetService<ILogger<FeatureEnablement>>());
            });

            // Why beside the features (FDW-1575): retention policies and legal holds are kept in the same tenant schema, in the tables the
            // platform declares for them. The keeper runs an organization's policies against the application's IRetainedDocuments, which the
            // application registers (the reference application does in its FileSystem connection type).
            builder.Services.AddScoped<IRetentionPolicies>(sp =>
            {
                var tenant = sp.GetRequiredService<SqlTenantConfiguration>();
                return new RetentionPolicies(
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    new RetentionTables(tenant.DataStoreName!, tenant.PathName!, "RetentionPolicy", "LegalHold"),
                    sp.GetService<ILogger<RetentionPolicies>>());
            });
            builder.Services.AddScoped<ILegalHolds>(sp =>
            {
                var tenant = sp.GetRequiredService<SqlTenantConfiguration>();
                return new LegalHolds(
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    new RetentionTables(tenant.DataStoreName!, tenant.PathName!, "RetentionPolicy", "LegalHold"),
                    sp.GetService<ILogger<LegalHolds>>());
            });
            builder.Services.AddScoped<IRetentionKeeper>(sp => new RetentionKeeper(
                sp.GetRequiredService<IRetentionPolicies>(),
                sp.GetRequiredService<ILegalHolds>(),
                sp.GetRequiredService<IRetainedDocuments>(),
                sp.GetService<ILogger<RetentionKeeper>>()));

            // Why: IOrganizationProvider reads tenant.Organization for org resolution and default-org
            // lookup. Registered here (not in Authorization) because it belongs to the multitenancy
            // domain and requires SqlTenantConfiguration for DataStore/path names.
            builder.Services.TryAddScoped<IOrganizationProvider>(sp =>
                new SqlOrganizationProvider(
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    sp.GetRequiredService<SqlTenantConfiguration>(),
                    sp.GetService<ILogger<SqlOrganizationProvider>>()));

            // Why: organizations are an FDW configuration domain (FDW-1010): tenant.Organization is the
            // domain row and tenant.OrganizationConfiguration its implementation row, read on the same
            // data store and schema as the other tenant tables, so both providers take their place from
            // SqlTenantConfiguration -- the one SqlOrganizationProvider reads. GET /organizations lists
            // the domain rows through the domain provider.
            builder.Services.TryAddSingleton<IOrganizationImplementationConfigurationProvider>(sp =>
            {
                var tenant = sp.GetRequiredService<SqlTenantConfiguration>();
                return new OrganizationImplementationConfigurationProvider(
                    sp.GetRequiredService<ILogger<OrganizationImplementationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    tenant.DataStoreName!, tenant.PathName!, "OrganizationConfiguration");
            });
            builder.Services.TryAddSingleton<OrganizationConfigurationProvider>(sp =>
            {
                var tenant = sp.GetRequiredService<SqlTenantConfiguration>();
                var domain = new OrganizationConfigurationProvider(
                    sp.GetRequiredService<ILogger<OrganizationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    tenant.DataStoreName!, tenant.PathName!, "Organization");
                domain.Register("Organization", sp.GetRequiredService<IOrganizationImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IOrganizationConfigurationProvider>(sp => sp.GetRequiredService<OrganizationConfigurationProvider>());
            // TenantOrgAccessConfigurationProvider selects its own gateway now, so it takes the
            // gateway provider. Singleton — it holds no per-request state.
            builder.Services.TryAddSingleton<TenantOrgAccessConfigurationProvider>();

            // Why: IOrgAccessProvider reads tenant.TenantOrgAccess for the org-tier grants used by
            // DefaultAuthorizationService and UserPermissions. Singleton: the provider
            // holds only singleton configuration infrastructure; user, tenant and org are call arguments.
            builder.Services.TryAddSingleton<IOrgAccessProvider>(sp =>
                new DefaultOrgAccessProvider(
                    sp.GetRequiredService<TenantOrgAccessConfigurationProvider>(),
                    sp.GetService<ILogger<DefaultOrgAccessProvider>>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

    }

    /// <inheritdoc/>
    public override bool EnablesTenantResolution => true;

    /// <summary>
    /// The <c>settings.SqlTenantProviderConfiguration</c> row is NOT read during Configure/Register — Phase 1 runs before the
    /// ServiceProvider is built, so <c>IConfigurationGateway</c> is not yet resolvable. It is instead
    /// read through the gateway-backed <see cref="SqlTenantConfiguration"/> singleton registered in
    /// the Register phase and forced during the Initialize phase (the post-Build
    /// fail-fast phase) — a missing row fails loud there, not on the first request.
    /// </summary>

    /// <summary>Phase 2: register the always-on contexts plus the SQL-backed tenant/org/org-access providers.</summary>


    // Why: extracted from the Registration phase body — the ImplementationConfigurationProviderBase.Get() read is
    // blocked-on synchronously exactly once (forced by Initialize, the post-Build fail-fast phase; see
    // ConfigurationGatewayDataStoreProvider.Initialize for the same sanctioned sync-over-async seam).
    // A missing/empty settings.SqlTenantProviderConfiguration result is a startup misconfiguration — fail loud
    // rather than construct an empty/default SqlTenantConfiguration (NO FALLBACKS).
    private static SqlTenantConfiguration ResolveTenantConfiguration(IServiceProvider sp)
    {
        var provider = sp.GetRequiredService<IMultitenancyConfigurationProvider>();
        var logger = sp.GetService<ILoggerFactory>()?.CreateLogger("SqlMultitenancyType") ?? NullLogger.Instance;

        // Why: this read has no ambient IAuthenticationContext -- it runs during the post-Build
        // Initialize phase, before any request/user is involved. Falling through to the deny
        // principal (AuthConstants.NoAccessPrincipalId) rather than elevating here only happened
        // to work because settings.SqlTenantProviderConfiguration is shared/system data (visible to every
        // authenticated caller under fn_TenantFilter's shared-row branch) -- it was never actually
        // reading as a trusted system operation the way TokenRevocationStore.IsRevoked and
        // ClaimMappedProvisioner.Provision do. Elevating explicitly is correct regardless of
        // whether the row happens to be shared today.
        using var systemScope = new SystemAuthenticationContextScope(sp.GetRequiredService<IAuthenticationContextAccessor>());

#pragma warning disable VSTHRD002
        var result = provider.Get(DomainName).GetAwaiter().GetResult();
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

        if (result.Value is not SqlTenantConfiguration configuration)
        {
            throw new InvalidOperationException(
                SqlMultitenancyLog.TenantProviderRowMissing(logger).Message);
        }

        return configuration;
    }
}
