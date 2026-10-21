using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using FdwReferenceMultitenancy.Sql.Logging;
using FdwReferenceMultitenancy.Sql.Models;
using FdwReferenceMultitenancy.Sql.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DeleteCommands = Fdw.Commands.Data.Extensions.Delete;
using UpdateCommands = Fdw.Commands.Data.Extensions.Update;

namespace FdwReferenceMultitenancy.Sql.Services;

/// <summary>
/// Writes organizations to the organization tables of the SQL-backed multitenancy option (FDW-1758), beside the reads of
/// <see cref="SqlOrganizationProvider"/>.
/// </summary>
/// <remarks>
/// An organization is a domain row in <c>tenant.Organization</c> naming its implementation, then an implementation row in
/// <c>tenant.OrganizationConfiguration</c> holding the key to it, as the seed makes the default organization. The writer refuses only what
/// would damage the organizations themselves; who may make one, its roles and its first administrator are settled by the caller first.
/// </remarks>
public sealed class SqlOrganizationWriter : IOrganizationWriter
{
    private const string OrganizationTable = "Organization";
    private const string ConfigurationTable = "OrganizationConfiguration";
    private const string DomainName = "Organization";

    private readonly IConfigurationGatewayProvider _configurationGateways;
    private readonly IOrganizationProvider _organizations;
    private readonly SqlTenantConfiguration _configuration;
    private readonly ILogger<SqlOrganizationWriter> _logger;

    /// <summary>Initializes a new instance of the <see cref="SqlOrganizationWriter"/> class.</summary>
    /// <param name="configurationGateways">Supplies the configuration gateway for the tables.</param>
    /// <param name="organizations">Reads the organizations.</param>
    /// <param name="configuration">The tenant provider configuration, which names the store, path and tenants table.</param>
    /// <param name="logger">Optional logger instance.</param>
    public SqlOrganizationWriter(
        IConfigurationGatewayProvider configurationGateways,
        IOrganizationProvider organizations,
        SqlTenantConfiguration configuration,
        ILogger<SqlOrganizationWriter>? logger = null)
    {
        _configurationGateways = configurationGateways ?? throw new ArgumentNullException(nameof(configurationGateways));
        _organizations = organizations ?? throw new ArgumentNullException(nameof(organizations));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? NullLogger<SqlOrganizationWriter>.Instance;
    }

    // The store and path the tables are in, as the provider's configuration names them.
    private sealed record Store(string DataStore, string Path);

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> Create(NewOrganization organization, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organization);
        if (string.IsNullOrWhiteSpace(organization.Name))
            return GenericResult<Guid>.Failure(SqlTenantResultCodes.ByName("OrganizationNameRequired"));
        if (string.IsNullOrWhiteSpace(organization.Slug))
            return GenericResult<Guid>.Failure(SqlTenantResultCodes.ByName("OrganizationSlugRequired"));

        var checkedInput = await CheckNew(organization, cancellationToken).ConfigureAwait(false);
        if (checkedInput.IsFailure)
            return checkedInput.ToNewResult<Guid>();

        var id = Guid.CreateVersion7();
        var inserted = await Write(id, store => Insert.Into<SqlOrganizationInsert>(OrganizationTable)
            .DataStore(store.DataStore)
            .Path(store.Path)
            .Value(new SqlOrganizationInsert
            {
                Id = id,
                Name = organization.Name,
                Domain = DomainName,
                Implementation = DomainName,
                TenantId = organization.TenantId,
                TenantRowId = checkedInput.Value,
                Slug = organization.Slug,
                IsDefault = false,
                IsAdminOrg = false,
                IsActive = true,
            }), cancellationToken).ConfigureAwait(false);
        if (inserted.IsFailure)
            return inserted.ToNewResult<Guid>();

        var linked = await Link(id, organization.TenantId, cancellationToken).ConfigureAwait(false);
        if (linked.IsFailure)
        {
            await Remove(id, cancellationToken).ConfigureAwait(false);
            return linked.ToNewResult<Guid>();
        }

        SqlOrganizationWriterLog.OrganizationMade(_logger, id, organization.Slug, organization.TenantId);
        return GenericResult<Guid>.Success(id);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Update(Guid organizationId, OrganizationChange change, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(change);
        if (change.Name is { } stated && string.IsNullOrWhiteSpace(stated))
            return GenericResult.Failure(SqlTenantResultCodes.ByName("OrganizationNameRequired"));

        var current = await _organizations.Get(organizationId, cancellationToken).ConfigureAwait(false);
        if (current.IsFailure || current.Value is not { } organization)
            return GenericResult.Failure(SqlTenantResultCodes.ByName("OrganizationNotFound"), ResultDetails.Create().With("OrganizationId", organizationId.ToString()));
        if (organization.IsDefault && change.IsActive == false)
            return GenericResult.Failure(SqlTenantResultCodes.ByName("DefaultOrganizationProtected"), ResultDetails.Create().With("OrganizationId", organizationId.ToString()));

        // Why the unstated parts are read from the organization: one write carries both columns, and a part the change does not state keeps its value.
        var written = await Write(organizationId, store => UpdateCommands.In<SqlOrganizationUpdate>(OrganizationTable)
            .DataStore(store.DataStore)
            .Path(store.Path)
            .Where("Id", organizationId)
            .Value(new SqlOrganizationUpdate
            {
                Name = change.Name is { } name ? name : organization.Name,
                IsActive = change.IsActive is { } active ? active : organization.IsActive,
            }), cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
            return written;

        SqlOrganizationWriterLog.OrganizationChanged(_logger, organizationId);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Delete(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var current = await _organizations.Get(organizationId, cancellationToken).ConfigureAwait(false);
        if (current.IsFailure || current.Value is not { } organization)
            return GenericResult.Failure(SqlTenantResultCodes.ByName("OrganizationNotFound"), ResultDetails.Create().With("OrganizationId", organizationId.ToString()));
        if (organization.IsDefault)
            return GenericResult.Failure(SqlTenantResultCodes.ByName("DefaultOrganizationProtected"), ResultDetails.Create().With("OrganizationId", organizationId.ToString()));

        var removed = await Remove(organizationId, cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
            return removed;

        SqlOrganizationWriterLog.OrganizationRemoved(_logger, organizationId);
        return GenericResult.Success();
    }

    // Removes an organization's two rows. Why the implementation row first: it points at the domain row, which cannot go while it is pointed at.
    private async Task<IGenericResult> Remove(Guid organizationId, CancellationToken cancellationToken)
    {
        var row = await RowOf(organizationId, cancellationToken).ConfigureAwait(false);
        if (row.IsFailure)
            return row;

        var detail = await Write(organizationId, store => DeleteCommands.From(ConfigurationTable)
            .DataStore(store.DataStore)
            .Path(store.Path)
            .Where("OrganizationRowId", row.Value)
            .Build(), cancellationToken, expectedRows: null).ConfigureAwait(false);
        if (detail.IsFailure)
            return detail;

        return await Write(organizationId, store => DeleteCommands.From(OrganizationTable)
            .DataStore(store.DataStore)
            .Path(store.Path)
            .Where("Id", organizationId)
            .Build(), cancellationToken).ConfigureAwait(false);
    }

    // What must be true of a new organization before it is written: its tenant exists and no organization of the tenant has its slug.
    // Success carries the key of the tenant's row, which the organization row points at.
    private async Task<IGenericResult<int>> CheckNew(NewOrganization organization, CancellationToken cancellationToken)
    {
        if (_configuration is not { DataStoreName: { } dataStore, PathName: { } path, TenantsTableName: { } tenants })
            return WriteFailed<int>(Guid.Empty, "the tenant provider configuration does not name the store, path and tenants table");

        var gateway = _configurationGateways.Get(dataStore);
        if (gateway.IsFailure || gateway.Value is not { } resolved)
            return WriteFailed<int>(Guid.Empty, gateway.CurrentMessage);

        var tenant = await resolved.Execute<IEnumerable<SqlTenantRow>>(
            Query.From<SqlTenantRow>(dataStore, path, tenants).Where("Id", organization.TenantId).Build(), cancellationToken).ConfigureAwait(false);
        if (!tenant.IsSuccess || tenant.Value is null)
            return WriteFailed<int>(Guid.Empty, tenant.CurrentMessage);
        var tenantRow = tenant.Value.FirstOrDefault();
        if (tenantRow is null)
            return GenericResult<int>.Failure(SqlTenantResultCodes.ByName("TenantNotFound"), ResultDetails.Create().With("TenantId", organization.TenantId.ToString()));

        var existing = await resolved.Execute<IEnumerable<SqlOrganizationEntity>>(
            Query.From<SqlOrganizationEntity>(dataStore, path, OrganizationTable)
                .Where("TenantId", organization.TenantId).Where("IsCurrent", true).Where("IsDeleted", false).Build(), cancellationToken).ConfigureAwait(false);
        if (!existing.IsSuccess || existing.Value is null)
            return WriteFailed<int>(Guid.Empty, existing.CurrentMessage);

        return existing.Value.Any(other => string.Equals(other.Slug, organization.Slug, StringComparison.OrdinalIgnoreCase))
            ? GenericResult<int>.Failure(SqlTenantResultCodes.ByName("OrganizationSlugTaken"), ResultDetails.Create().With("Slug", organization.Slug))
            : GenericResult<int>.Success(tenantRow.RowId);
    }

    // Writes the implementation row that points at the organization's domain row.
    private async Task<IGenericResult> Link(Guid organizationId, Guid tenantId, CancellationToken cancellationToken)
    {
        var row = await RowOf(organizationId, cancellationToken).ConfigureAwait(false);
        if (row.IsFailure)
            return row;

        return await Write(organizationId, store => Insert.Into<SqlOrganizationConfigurationInsert>(ConfigurationTable)
            .DataStore(store.DataStore)
            .Path(store.Path)
            .Value(new SqlOrganizationConfigurationInsert { OrganizationRowId = row.Value, TenantId = tenantId }), cancellationToken).ConfigureAwait(false);
    }

    // The key of an organization's domain row.
    private async Task<IGenericResult<int>> RowOf(Guid organizationId, CancellationToken cancellationToken)
    {
        if (_configuration is not { DataStoreName: { } dataStore, PathName: { } path })
            return WriteFailed<int>(organizationId, "the tenant provider configuration does not name the store and path");

        var gateway = _configurationGateways.Get(dataStore);
        if (gateway.IsFailure || gateway.Value is not { } resolved)
            return WriteFailed<int>(organizationId, gateway.CurrentMessage);

        var read = await resolved.Execute<IEnumerable<SqlOrganizationRow>>(
            Query.From<SqlOrganizationRow>(dataStore, path, OrganizationTable).Where("Id", organizationId).Build(), cancellationToken).ConfigureAwait(false);
        if (!read.IsSuccess || read.Value is null)
            return WriteFailed<int>(organizationId, read.CurrentMessage);

        var row = read.Value.FirstOrDefault();
        return row is null
            ? GenericResult<int>.Failure(SqlTenantResultCodes.ByName("OrganizationNotFound"), ResultDetails.Create().With("OrganizationId", organizationId.ToString()))
            : GenericResult<int>.Success(row.RowId);
    }

    // Runs one write; a write that touches no row is a failure unless the caller states it may touch none.
    private async Task<IGenericResult> Write(Guid organizationId, Func<Store, DataGatewayCall> command, CancellationToken cancellationToken, int? expectedRows = 1)
    {
        if (_configuration is not { DataStoreName: { } dataStore, PathName: { } path })
            return WriteFailed(organizationId, "the tenant provider configuration does not name the store and path");

        var gateway = _configurationGateways.Get(dataStore);
        if (gateway.IsFailure || gateway.Value is not { } resolved)
            return WriteFailed(organizationId, gateway.CurrentMessage);

        var executed = await resolved.Execute<int>(command(new Store(dataStore, path)), cancellationToken).ConfigureAwait(false);
        if (!executed.IsSuccess)
            return WriteFailed(organizationId, executed.CurrentMessage);

        return expectedRows is { } expected && executed.Value != expected
            ? WriteFailed(organizationId, $"{executed.Value} rows were written, not {expected}")
            : GenericResult.Success();
    }

    // The error detail is added only when the failed result carried a message; none is made up.
    private static IGenericResult WriteFailed(Guid organizationId, string? reason)
    {
        var details = ResultDetails.Create().With("OrganizationId", organizationId.ToString());
        return GenericResult.Failure(SqlTenantResultCodes.ByName("OrganizationWriteFailed"), reason is { } text ? details.With("Error", text) : details);
    }

    private static IGenericResult<T> WriteFailed<T>(Guid organizationId, string? reason) => WriteFailed(organizationId, reason).ToNewResult<T>();
}
