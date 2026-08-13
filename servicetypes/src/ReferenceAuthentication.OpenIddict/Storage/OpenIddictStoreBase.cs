using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Results.Abstractions;
using ReferenceAuthentication.OpenIddict.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CmdBuilders = Fdw.Commands.Data.Extensions;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Storage;

/// <summary>
/// Base class for DataGateway-backed OpenIddict stores.
/// All data operations go through <see cref="IDataGateway"/> using the fluent builder surface
/// (<c>CmdBuilders.Insert.Into&lt;T&gt;(…).DataStore(…).Path(…).Value(…)</c>).
/// Internal results use <see cref="IGenericResult"/> + MessageLogging. Exceptions surface via
/// <see cref="Unwrap"/>/<see cref="UnwrapValue{TValue}"/> only at the store-method boundary
/// where OpenIddict's contract requires exceptions, not results.
/// </summary>
internal abstract class OpenIddictStoreBase
{
    /// <summary>AuthDb data store name — matches the logical connection name in configurationSchema.json.</summary>
    protected const string DataStoreName = "AuthDb";

    /// <summary>Schema path within AuthDb that contains the OpenIddict tables.</summary>
    protected const string PathName = "auth";

    // Why: Lazy<IDataGateway> defers DataGateway resolution to first use so startup registration
    // order does not matter — the gateway is not needed until the first store operation.
    private readonly Lazy<IDataGateway> _dataGateway;

    /// <summary>Logger instance; never null — falls back to NullLogger.</summary>
    protected readonly ILogger<OpenIddictStoreBase> Logger;

    /// <summary>Initializes a new instance of <see cref="OpenIddictStoreBase"/>.</summary>
    // Why the accessor is here: every read below happens before anyone is authenticated - validating a
    // client, finding a scope, checking a token - so there is no principal to filter rows by. Row-level
    // security sets SESSION_CONTEXT UserId to the reserved no-access principal when no context is
    // established, and its predicate then matches nothing, so these queries returned zero rows and the
    // AuthDb connection came back unresolvable. The predicate's own system-bypass mode is UserId IS
    // NULL, which is exactly what SystemAuthenticationContextScope produces.
    private readonly IAuthenticationContextAccessor _authContext;

    protected OpenIddictStoreBase(
        Lazy<IDataGateway> dataGateway,
        IAuthenticationContextAccessor authContext,
        ILogger<OpenIddictStoreBase>? logger)
    {
        ArgumentNullException.ThrowIfNull(dataGateway);
        ArgumentNullException.ThrowIfNull(authContext);
        _dataGateway = dataGateway;
        _authContext = authContext;
        Logger = logger ?? NullLogger<OpenIddictStoreBase>.Instance;
    }

    /// <summary>Runs the store's own reads and writes as the system, not as the caller.</summary>
    /// <returns>A scope to dispose when the operation completes.</returns>
    protected SystemAuthenticationContextScope AsSystem() => new(_authContext);

    // ── Protected helpers ─────────────────────────────────────────────────────────────

    /// <summary>Exposes the resolved gateway to concrete stores.</summary>
    protected IDataGateway Gateway => _dataGateway.Value;

    /// <summary>
    /// Executes a query and returns the typed result set.
    /// Returns a non-success <see cref="IGenericResult{T}"/> (with MessageLogging) on gateway failure.
    /// </summary>
    /// <remarks>
    /// Accepts a <see cref="DataGatewayCall"/> (the value returned by fluent builder terminal methods)
    /// so that addressing (DataStore/Path/Container) travels with the command, not on it.
    /// </remarks>
    protected async Task<IGenericResult<IEnumerable<T>>> QueryCurrent<T>(
        DataGatewayCall call,
        CancellationToken cancellationToken)
        where T : class
    {
        using var systemScope = AsSystem();
        var result = await _dataGateway.Value
            .Execute<IEnumerable<T>>(call, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult<IEnumerable<T>>.Failure(
                OpenIddictStoreLog.DataGatewayQueryFailed(
                    Logger, result.CurrentMessage!));
        }

        return GenericResult<IEnumerable<T>>.Success(result.Value ?? []);
    }

    /// <summary>Inserts a single row into a container. Returns a non-success result on gateway failure.</summary>
    protected async Task<IGenericResult> InsertVersion<T>(
        string containerName,
        T record,
        CancellationToken cancellationToken)
        where T : class
    {
        var command = CmdBuilders.Insert.Into<T>(containerName)
            .DataStore(DataStoreName)
            .Path(PathName)
            .Value(record);

        using var systemScope = AsSystem();
        var result = await _dataGateway.Value
            .Execute<int>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult.Failure(
                OpenIddictStoreLog.DataGatewayInsertFailed(
                    Logger, result.CurrentMessage!));
        }

        return GenericResult.Success();
    }

    /// <summary>
    /// Inserts a list of child rows into a container (one insert per row).
    /// Returns a non-success result on first gateway failure.
    /// </summary>
    protected async Task<IGenericResult> InsertChildSet<T>(
        string containerName,
        IReadOnlyList<T> records,
        CancellationToken cancellationToken)
        where T : class
    {
        // Why: Row-by-row insert. Child sets are always small (resources, permissions, redirect URIs)
        // so the simplicity outweighs the overhead of a batch insert for Phase 1.
        foreach (var record in records)
        {
            var insertResult = await InsertVersion(containerName, record, cancellationToken)
                .ConfigureAwait(false);
            if (!insertResult.IsSuccess)
            {
                return insertResult;
            }
        }

        return GenericResult.Success();
    }

    /// <summary>
    /// Updates a single row in-place by logical Id. All mapped properties on <typeparamref name="T"/>
    /// are included in the SET clause (equivalent to a full-row UPDATE).
    /// </summary>
    protected async Task<IGenericResult> UpdateRecord<T>(
        string containerName,
        string idColumnName,
        Guid id,
        T record,
        CancellationToken cancellationToken)
        where T : class
    {
        var command = CmdBuilders.Update.In<T>(containerName)
            .DataStore(DataStoreName)
            .Path(PathName)
            .Where(idColumnName, id)
            .Value(record);

        using var systemScope = AsSystem();
        var result = await _dataGateway.Value
            .Execute<int>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult.Failure(
                OpenIddictStoreLog.DataGatewayUpdateFailed(
                    Logger, result.CurrentMessage!));
        }

        return GenericResult.Success();
    }

    /// <summary>
    /// Deletes all rows matching <paramref name="idColumnName"/> = <paramref name="id"/>.
    /// Used for both hard-delete of a single entity row and delete-all-children operations.
    /// </summary>
    protected async Task<IGenericResult> DeleteRecord(
        string containerName,
        string idColumnName,
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = CmdBuilders.Delete.From(containerName)
            .DataStore(DataStoreName)
            .Path(PathName)
            .Where(idColumnName, id)
            .Build();

        using var systemScope = AsSystem();
        var result = await _dataGateway.Value
            .Execute<int>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult.Failure(
                OpenIddictStoreLog.DataGatewayDeleteFailed(
                    Logger, result.CurrentMessage!));
        }

        return GenericResult.Success();
    }

    // Why: Minimal value type for supersede updates. Using the full record type for the UPDATE value would
    // set ALL fields (including ScopeId, Resource, Name, etc.) to their defaults, corrupting data.
    // By using a type that ONLY has IsCurrent (and IsDeleted), the translator only generates
    // SET [IsCurrent]=0 (and SET [IsDeleted]=1), leaving all other columns untouched.
    private sealed class SupersedeValue { public bool IsCurrent { get; set; } }
    private sealed class SupersedeWithDeleteValue { public bool IsCurrent { get; set; } public bool IsDeleted { get; set; } }

    /// <summary>
    /// Sets <c>IsCurrent=0</c> on all current rows for a parent logical Id.
    /// </summary>
    protected Task<IGenericResult> SupersedeCurrent(
        string containerName,
        string parentIdColumnName,
        Guid parentId,
        CancellationToken cancellationToken)
        => ExecuteSupersede(containerName, parentIdColumnName, parentId,
            new SupersedeValue { IsCurrent = false }, cancellationToken);

    /// <summary>
    /// Sets <c>IsCurrent=0, IsDeleted=1</c> on all current rows for a parent logical Id (soft-delete).
    /// </summary>
    protected Task<IGenericResult> SupersedeCurrentAndDelete(
        string containerName,
        string parentIdColumnName,
        Guid parentId,
        CancellationToken cancellationToken)
        => ExecuteSupersede(containerName, parentIdColumnName, parentId,
            new SupersedeWithDeleteValue { IsCurrent = false, IsDeleted = true }, cancellationToken);

    private async Task<IGenericResult> ExecuteSupersede<T>(
        string containerName,
        string parentIdColumnName,
        Guid parentId,
        T supersededValue,
        CancellationToken cancellationToken)
        where T : class
    {
        // Why: parentIdColumnName is either the entity's own Id (for parent rows) or ScopeId (for children).
        // The WHERE on IsCurrent=1 ensures only the live row is superseded, not archived versions.
        var command = CmdBuilders.Update.In<T>(containerName)
            .DataStore(DataStoreName)
            .Path(PathName)
            .Where(parentIdColumnName, parentId)
            .Where("IsCurrent", true)
            .Value(supersededValue);

        using var systemScope = AsSystem();
        var result = await _dataGateway.Value
            .Execute<int>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult.Failure(
                OpenIddictStoreLog.DataGatewaySupersedeFailed(
                    Logger, result.CurrentMessage!));
        }

        return GenericResult.Success();
    }

    // ── Boundary unwrap ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Unwraps a non-generic <see cref="IGenericResult"/>.
    /// On failure, throws <see cref="InvalidOperationException"/> with the failure message.
    /// Call ONLY at the store-method boundary where OpenIddict's contract requires exceptions.
    /// </summary>
    protected static void Unwrap(IGenericResult result)
    {
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                result.CurrentMessage!);
        }
    }

    /// <summary>
    /// Unwraps a typed <see cref="IGenericResult{TValue}"/> and returns the value.
    /// On failure, throws <see cref="InvalidOperationException"/> with the failure message.
    /// Call ONLY at the store-method boundary where OpenIddict's contract requires exceptions.
    /// </summary>
    protected static TValue UnwrapValue<TValue>(IGenericResult<TValue> result)
    {
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                result.CurrentMessage!);
        }

        return result.Value!;
    }
}
