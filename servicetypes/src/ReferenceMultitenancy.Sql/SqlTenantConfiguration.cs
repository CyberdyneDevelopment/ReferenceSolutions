using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace ReferenceMultitenancy.Sql;

/// <summary>
/// Configuration for the SQL-backed tenant provider — the single row in
/// <c>settings.SqlTenantProvider</c>, read through
/// <see cref="Fdw.Services.Configuration.DefaultConfigurationProvider{TConfig,TCommand}"/> over
/// <see cref="Fdw.Services.Data.Abstractions.IConfigurationGateway"/>. Resolved once, as a singleton
/// forced during the Multitenancy domain's post-Build <c>SqlMultitenancyType.Initialize</c> (ReferenceMultitenancy.Sql)
/// fail-fast phase — never bound from <c>IConfiguration</c> (see
/// <c>SqlMultitenancyLog</c> (ReferenceMultitenancy.Sql)).
/// Why: Tenant definitions are system infrastructure. Users cannot create/delete tenants
/// through cfg CRUD — that is a system admin operation managed through settings schema and seed data.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "TenantProvider")]
public sealed partial class SqlTenantConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the configuration name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string SectionName => "SqlTenantProvider";

    /// <inheritdoc/>
    public string ServiceType => "TenantProvider";

    /// <inheritdoc/>
    public string? ServiceOptionType => null;

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
