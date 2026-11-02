using Fdw.Services.Multitenancy;
using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace FdwReferenceMultitenancy.Sql;

/// <summary>
/// Configuration for the SQL-backed tenant provider — the single row in
/// <c>settings.SqlTenantProviderConfiguration</c>, read through
/// <c>ImplementationConfigurationProviderBase{TConfig,TCommand}</c> over
/// <see cref="Fdw.Services.Data.Abstractions.IConfigurationGateway"/>. Resolved once, as a singleton
/// forced during the Multitenancy domain's post-Build <c>SqlMultitenancyType.Initialize</c> (FdwReferenceMultitenancy.Sql)
/// fail-fast phase — never bound from <c>IConfiguration</c> (see
/// <c>SqlMultitenancyLog</c> (FdwReferenceMultitenancy.Sql)).
/// Why: Tenant definitions are system infrastructure. They are made, changed and removed only through
/// <see cref="Fdw.Services.Multitenancy.Abstractions.ITenantWriter"/> (FDW-1578), which a platform administrator reaches through
/// the tenant endpoints; the seed supplies the first tenant.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "TenantProvider")]
public sealed partial class SqlTenantConfiguration : IMultitenancyImplementationConfiguration
{
    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>The Multitenancy domain row's Id.</summary>
    /// <remarks>
    /// An implementation has no identity of its own: this is the domain's Id, set by the provider from
    /// the domain row it joined to on MultitenancyRowId, and never persisted on the implementation row.
    /// A second id alongside it -- SqlTenantProviderId, named for the implementation's table and
    /// populated by nothing -- used to sit here and claimed to be "the domain record's durable id";
    /// it was neither, and is gone.
    /// </remarks>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the configuration name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DataStore name for tenant data.
    /// </summary>
    public string? DataStoreName { get; set; }

    /// <summary>
    /// Gets or sets the path name (schema) within the DataStore.
    /// </summary>
    public string? PathName { get; set; }

    /// <summary>
    /// Gets or sets the tenants table name.
    /// </summary>
    public string? TenantsTableName { get; set; }

    /// <summary>
    /// Gets or sets the tenant features table name.
    /// </summary>
    public string? TenantFeaturesTableName { get; set; }

    /// <summary>
    /// Gets or sets the tenant settings table name.
    /// </summary>
    public string? TenantSettingsTableName { get; set; }

    /// <summary>
    /// Gets or sets the tenant roles table name.
    /// </summary>
    public string? TenantRolesTableName { get; set; }

    /// <summary>
    /// Gets or sets the user-tenant mapping table name.
    /// </summary>
    public string? UserTenantsTableName { get; set; }
}
