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
/// Writes tenants to the tenants table of the SQL-backed multitenancy option (FDW-1578), beside the reads of
/// <see cref="SqlTenantProvider"/>.
/// </summary>
/// <remarks>
/// A tenant is read back through <see cref="ITenantProvider"/>, so a change is judged against the tenant as the rest of the system
/// sees it. The writer refuses only what would damage the tenants themselves; who may make one, who becomes its first administrator and
/// what must be gone before a delete are settled by the caller first.
/// </remarks>
public sealed class SqlTenantWriter : ITenantWriter
{
    private readonly IConfigurationGatewayProvider _configurationGateways;
    private readonly ITenantProvider _tenants;
    private readonly SqlTenantConfiguration _configuration;
    private readonly ILogger<SqlTenantWriter> _logger;

    /// <summary>Initializes a new instance of the <see cref="SqlTenantWriter"/> class.</summary>
    /// <param name="configurationGateways">Supplies the configuration gateway for the tenants table.</param>
    /// <param name="tenants">Reads the tenants.</param>
    /// <param name="configuration">The tenant provider configuration, which names the store, path and table.</param>
    /// <param name="logger">Optional logger instance.</param>
    public SqlTenantWriter(
        IConfigurationGatewayProvider configurationGateways,
        ITenantProvider tenants,
        SqlTenantConfiguration configuration,
        ILogger<SqlTenantWriter>? logger = null)
    {
        _configurationGateways = configurationGateways ?? throw new ArgumentNullException(nameof(configurationGateways));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? NullLogger<SqlTenantWriter>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> Create(NewTenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        if (string.IsNullOrWhiteSpace(tenant.Name))
            return GenericResult<Guid>.Failure(SqlTenantResultCodes.ByName("TenantNameRequired"));
        if (string.IsNullOrWhiteSpace(tenant.Slug))
            return GenericResult<Guid>.Failure(SqlTenantResultCodes.ByName("TenantSlugRequired"));

        var all = await _tenants.GetAllTenants(cancellationToken).ConfigureAwait(false);
        if (all.IsFailure || all.Value is null)
            return WriteFailed(Guid.Empty, all.CurrentMessage).ToNewResult<Guid>();
        if (all.Value.Any(existing => string.Equals(existing.Slug, tenant.Slug, StringComparison.OrdinalIgnoreCase)))
            return GenericResult<Guid>.Failure(SqlTenantResultCodes.ByName("TenantSlugTaken"), ResultDetails.Create().With("Slug", tenant.Slug));

        var id = Guid.CreateVersion7();
        var written = await Write(id, store => Insert.Into<SqlTenantInsert>(store.Table)
            .DataStore(store.DataStore)
            .Path(store.Path)
            .Value(new SqlTenantInsert
            {
                Id = id,
                Slug = tenant.Slug,
                OrgPrefix = tenant.OrgPrefix,
                DisplayName = tenant.Name,
                IsActive = true,
                IsGlobal = false,
                IsSelectable = true,
                ConnectionName = tenant.ConnectionName,
            }), cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
            return written.ToNewResult<Guid>();

        SqlTenantWriterLog.TenantMade(_logger, id, tenant.Slug);
        return GenericResult<Guid>.Success(id);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Update(Guid tenantId, TenantChange change, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(change);
        if (change.Name is { } stated && string.IsNullOrWhiteSpace(stated))
            return GenericResult.Failure(SqlTenantResultCodes.ByName("TenantNameRequired"));

        var current = await _tenants.GetTenant(tenantId, cancellationToken).ConfigureAwait(false);
        if (current.IsFailure || current.Value is null)
            return current;
        var tenant = current.Value;
        if (tenant.IsGlobal && change.IsActive == false)
            return GenericResult.Failure(SqlTenantResultCodes.ByName("GlobalTenantProtected"), ResultDetails.Create().With("TenantId", tenantId.ToString()));

        // Why the unstated parts are read from the tenant: one write carries all three columns, and a part the change does not state keeps its value.
        var written = await Write(tenantId, store => UpdateCommands.In<SqlTenantUpdate>(store.Table)
            .DataStore(store.DataStore)
            .Path(store.Path)
            .Where("Id", tenantId)
            .Value(new SqlTenantUpdate
            {
                DisplayName = change.Name is { } name ? name : tenant.DisplayName,
                IsActive = change.IsActive is { } active ? active : tenant.IsActive,
                ConnectionName = change.ConnectionName is { } connection ? connection : tenant.ConnectionName,
            }), cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
            return written;

        SqlTenantWriterLog.TenantChanged(_logger, tenantId);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Delete(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var current = await _tenants.GetTenant(tenantId, cancellationToken).ConfigureAwait(false);
        if (current.IsFailure || current.Value is null)
            return current;
        if (current.Value.IsGlobal)
            return GenericResult.Failure(SqlTenantResultCodes.ByName("GlobalTenantProtected"), ResultDetails.Create().With("TenantId", tenantId.ToString()));

        var written = await Write(tenantId, store => DeleteCommands.From(store.Table)
            .DataStore(store.DataStore)
            .Path(store.Path)
            .Where("Id", tenantId)
            .Build(), cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
            return written;

        SqlTenantWriterLog.TenantRemoved(_logger, tenantId);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult> SetDeleted(Guid tenantId, bool isDeleted, CancellationToken cancellationToken = default)
    {
        var state = await StateOf(tenantId, cancellationToken).ConfigureAwait(false);
        if (state.IsFailure || state.Value is not { } tenant)
            return state;
        if (tenant.IsGlobal)
            return GenericResult.Failure(SqlTenantResultCodes.ByName("GlobalTenantProtected"), ResultDetails.Create().With("TenantId", tenantId.ToString()));
        if (isDeleted && tenant.IsActive)
            return GenericResult.Failure(SqlTenantResultCodes.ByName("TenantStillActive"), ResultDetails.Create().With("TenantId", tenantId.ToString()));

        // Why a tenant already in the state asked for is a success that writes nothing: the answer the caller wants is already true.
        if (tenant.IsDeleted == isDeleted)
            return GenericResult.Success();

        var written = await Write(tenantId, store => UpdateCommands.In<SqlTenantDeletedUpdate>(store.Table)
            .DataStore(store.DataStore)
            .Path(store.Path)
            .Where("Id", tenantId)
            .Value(new SqlTenantDeletedUpdate { IsDeleted = isDeleted }), cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
            return written;

        SqlTenantWriterLog.TenantChanged(_logger, tenantId);
        return GenericResult.Success();
    }

    // The tenant row as it stands, deleted or not; the provider leaves deleted tenants out, so a restore reads it here.
    private async Task<IGenericResult<SqlTenantState>> StateOf(Guid tenantId, CancellationToken cancellationToken)
    {
        if (_configuration is not { DataStoreName: { } dataStore, PathName: { } path, TenantsTableName: { } table })
            return WriteFailed(tenantId, "the tenant provider configuration does not name the store, path and tenants table").ToNewResult<SqlTenantState>();

        var gateway = _configurationGateways.Get(dataStore);
        if (gateway.IsFailure || gateway.Value is not { } resolved)
            return WriteFailed(tenantId, gateway.CurrentMessage).ToNewResult<SqlTenantState>();

        var read = await resolved.Execute<IEnumerable<SqlTenantState>>(
            Query.From<SqlTenantState>(dataStore, path, table).Where("Id", tenantId).Build(), cancellationToken).ConfigureAwait(false);
        if (!read.IsSuccess || read.Value is null)
            return WriteFailed(tenantId, read.CurrentMessage).ToNewResult<SqlTenantState>();

        var tenant = read.Value.FirstOrDefault();
        return tenant is null
            ? GenericResult<SqlTenantState>.Failure(SqlTenantResultCodes.ByName("TenantNotFound"), ResultDetails.Create().With("TenantId", tenantId.ToString()))
            : GenericResult<SqlTenantState>.Success(tenant);
    }

    // The store, path and table the tenants are in, as the provider's configuration names them.
    private sealed record TenantStore(string DataStore, string Path, string Table);

    // Runs one write against the tenants table; a write that touches no row is a failure, because the tenant was just read and exists.
    private async Task<IGenericResult> Write(Guid tenantId, Func<TenantStore, DataGatewayCall> command, CancellationToken cancellationToken)
    {
        if (_configuration is not { DataStoreName: { } dataStore, PathName: { } path, TenantsTableName: { } table })
            return WriteFailed(tenantId, "the tenant provider configuration does not name the store, path and tenants table");

        var gateway = _configurationGateways.Get(dataStore);
        if (gateway.IsFailure || gateway.Value is null)
            return WriteFailed(tenantId, gateway.CurrentMessage);

        var executed = await gateway.Value.Execute<int>(command(new TenantStore(dataStore, path, table)), cancellationToken).ConfigureAwait(false);
        return executed.IsSuccess && executed.Value == 1
            ? GenericResult.Success()
            : WriteFailed(tenantId, executed.IsSuccess ? $"{executed.Value} rows were written, not one" : executed.CurrentMessage);
    }

    // The error detail is added only when the failed result carried a message; none is made up.
    private static IGenericResult WriteFailed(Guid tenantId, string? reason)
    {
        var details = ResultDetails.Create().With("TenantId", tenantId.ToString());
        return GenericResult.Failure(SqlTenantResultCodes.ByName("TenantWriteFailed"), reason is { } text ? details.With("Error", text) : details);
    }
}
