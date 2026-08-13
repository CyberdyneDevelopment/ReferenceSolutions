using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using ReferenceAuthentication.OpenIddict.Logging;
using ReferenceAuthentication.OpenIddict.Storage.Models;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using Query = Fdw.Commands.Data.DataQuery;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Storage;

/// <summary>
/// DataGateway-backed <see cref="IOpenIddictAuthorizationStore{TAuthorization}"/> over <see cref="OpenIddictAuthorizationRecord"/>.
/// Persists to <c>auth.OpenIddictAuthorization</c> (parent, plain update-in-place) and
/// <c>auth.OpenIddictAuthorizationScope</c> (child, delete-then-insert on SetScopesAsync).
/// Properties are omitted per Phase 1 design.
/// LINQ-based overloads (CountAsync&lt;TResult&gt;, GetAsync, ListAsync&lt;TState,TResult&gt;) throw <see cref="NotSupportedException"/>.
/// </summary>
internal sealed class OpenIddictAuthorizationStore : OpenIddictStoreBase, IOpenIddictAuthorizationStore<OpenIddictAuthorizationRecord>
{
    private const string AuthorizationContainer = "OpenIddictAuthorization";
    private const string AuthorizationScopeContainer = "OpenIddictAuthorizationScope";
    private const string RevokedStatus = "revoked";

    // Why: Same ConditionalWeakTable staging pattern as ScopeStore. [GenerateMapper] maps every
    // property on the record to a SQL column; staging state must NOT live on the record.
    private sealed class PendingScopes
    {
        public ImmutableArray<string>? Value { get; set; }
    }
    private static readonly ConditionalWeakTable<OpenIddictAuthorizationRecord, PendingScopes> _pendingScopes = new();

    /// <summary>Initializes a new instance of <see cref="OpenIddictAuthorizationStore"/>.</summary>
    public OpenIddictAuthorizationStore(Lazy<IDataGateway> dataGateway, IAuthenticationContextAccessor authContext, ILogger<OpenIddictAuthorizationStore>? logger)
        : base(dataGateway, authContext, logger)
    {
    }

    // ── Persistence ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask CreateAsync(OpenIddictAuthorizationRecord authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        OpenIddictStoreLog.AuthorizationCreateStarted(Logger);

        try
        {
            if (authorization.Id == Guid.Empty)
            {
                authorization.Id = Guid.NewGuid();
            }

            authorization.CreatedAt = DateTimeOffset.UtcNow;
            authorization.ModifiedAt = DateTimeOffset.UtcNow;
            if (authorization.CreationDate == default)
            {
                authorization.CreationDate = DateTimeOffset.UtcNow;
            }

            Unwrap(await InsertVersion(AuthorizationContainer, authorization, cancellationToken).ConfigureAwait(false));

            if (_pendingScopes.TryGetValue(authorization, out var pending) && pending.Value.HasValue)
            {
                Unwrap(await InsertScopeSet(authorization.Id, pending.Value.Value, cancellationToken).ConfigureAwait(false));
                _pendingScopes.Remove(authorization);
            }

            OpenIddictStoreLog.AuthorizationCreated(Logger, authorization.Id);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                OpenIddictStoreLog.AuthorizationCreateFailed(Logger, ex).Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask UpdateAsync(OpenIddictAuthorizationRecord authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        OpenIddictStoreLog.AuthorizationUpdateStarted(Logger, authorization.Id);

        try
        {
            // Why: Update-in-place. Modify the single parent row; no version row inserted.
            authorization.ModifiedAt = DateTimeOffset.UtcNow;
            Unwrap(await UpdateRecord(
                AuthorizationContainer,
                nameof(OpenIddictAuthorizationRecord.Id),
                authorization.Id,
                authorization,
                cancellationToken).ConfigureAwait(false));

            if (_pendingScopes.TryGetValue(authorization, out var pending) && pending.Value.HasValue)
            {
                // Why: Delete the entire child scope set then insert the new set.
                // No version history on scopes — plain delete+insert is correct for operational data.
                Unwrap(await DeleteRecord(
                    AuthorizationScopeContainer,
                    nameof(OpenIddictAuthorizationScopeRecord.AuthorizationId),
                    authorization.Id,
                    cancellationToken).ConfigureAwait(false));

                Unwrap(await InsertScopeSet(authorization.Id, pending.Value.Value, cancellationToken).ConfigureAwait(false));
                _pendingScopes.Remove(authorization);
            }

            OpenIddictStoreLog.AuthorizationUpdated(Logger, authorization.Id);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                OpenIddictStoreLog.AuthorizationUpdateFailed(Logger, ex, authorization.Id).Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask DeleteAsync(OpenIddictAuthorizationRecord authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        try
        {
            // Why: Delete child scope rows first (FK by AuthorizationId), then delete the parent.
            // Hard-delete — authorizations are operational data; no audit history is needed.
            Unwrap(await DeleteRecord(
                AuthorizationScopeContainer,
                nameof(OpenIddictAuthorizationScopeRecord.AuthorizationId),
                authorization.Id,
                cancellationToken).ConfigureAwait(false));

            Unwrap(await DeleteRecord(
                AuthorizationContainer,
                nameof(OpenIddictAuthorizationRecord.Id),
                authorization.Id,
                cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                OpenIddictStoreLog.AuthorizationDeleteFailed(Logger, ex, authorization.Id).Message, ex);
        }
    }

    // ── Count ───────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<long> CountAsync(CancellationToken cancellationToken)
    {
        var command = Query.From<OpenIddictAuthorizationRecord>(DataStoreName, PathName, AuthorizationContainer)
            .Build();

        return UnwrapValue(await QueryCurrent<OpenIddictAuthorizationRecord>(command, cancellationToken).ConfigureAwait(false)).LongCount();
    }

    /// <inheritdoc />
    public ValueTask<long> CountAsync<TResult>(
        Func<IQueryable<OpenIddictAuthorizationRecord>, IQueryable<TResult>> query,
        CancellationToken cancellationToken)
        => throw new NotSupportedException(
            "LINQ-based CountAsync is not supported by the DataGateway store. Use the plain CountAsync overload.");

    // ── Find ────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<OpenIddictAuthorizationRecord?> FindByIdAsync(
        string identifier,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(identifier, out var id))
        {
            return null;
        }

        var command = Query.From<OpenIddictAuthorizationRecord>(DataStoreName, PathName, AuthorizationContainer)
            .Where(r => r.Id).Equal(id)
            .Build();

        return UnwrapValue(await QueryCurrent<OpenIddictAuthorizationRecord>(command, cancellationToken).ConfigureAwait(false))
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<OpenIddictAuthorizationRecord> FindByApplicationIdAsync(
        string identifier,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(identifier, out var appId))
        {
            yield break;
        }

        var command = Query.From<OpenIddictAuthorizationRecord>(DataStoreName, PathName, AuthorizationContainer)
            .Where(r => r.ApplicationId).Equal(appId)
            .Build();

        foreach (var auth in UnwrapValue(await QueryCurrent<OpenIddictAuthorizationRecord>(command, cancellationToken).ConfigureAwait(false)))
        {
            yield return auth;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<OpenIddictAuthorizationRecord> FindBySubjectAsync(
        string subject,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(subject);

        var command = Query.From<OpenIddictAuthorizationRecord>(DataStoreName, PathName, AuthorizationContainer)
            .Where(r => r.Subject).Equal(subject)
            .Build();

        foreach (var auth in UnwrapValue(await QueryCurrent<OpenIddictAuthorizationRecord>(command, cancellationToken).ConfigureAwait(false)))
        {
            yield return auth;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<OpenIddictAuthorizationRecord> FindAsync(
        string? subject,
        string? client,
        string? status,
        string? type,
        ImmutableArray<string>? scopes,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var queryBuilder = Query.From<OpenIddictAuthorizationRecord>(DataStoreName, PathName, AuthorizationContainer);

        if (!string.IsNullOrEmpty(subject))
        {
            queryBuilder = queryBuilder.Where(r => r.Subject).Equal(subject);
        }

        if (!string.IsNullOrEmpty(client) && Guid.TryParse(client, out var clientId))
        {
            queryBuilder = queryBuilder.Where(r => r.ApplicationId).Equal(clientId);
        }

        if (!string.IsNullOrEmpty(status))
        {
            queryBuilder = queryBuilder.Where(r => r.Status).Equal(status);
        }

        if (!string.IsNullOrEmpty(type))
        {
            queryBuilder = queryBuilder.Where(r => r.AuthorizationType).Equal(type);
        }

        var results = UnwrapValue(await QueryCurrent<OpenIddictAuthorizationRecord>(queryBuilder.Build(), cancellationToken).ConfigureAwait(false)).ToList();

        // Why: Scope filtering requires a cross-check with the child AuthorizationScope table.
        // Only performed if scopes are specified; the parent query already narrows the candidate set.
        if (scopes.HasValue && !scopes.Value.IsEmpty)
        {
            foreach (var auth in results)
            {
                var authScopes = await GetScopesAsync(auth, cancellationToken).ConfigureAwait(false);
                if (scopes.Value.All(s => authScopes.Contains(s, StringComparer.OrdinalIgnoreCase)))
                {
                    yield return auth;
                }
            }
        }
        else
        {
            foreach (var auth in results)
            {
                yield return auth;
            }
        }
    }

    // ── GetAsync (LINQ — not supported) ─────────────────────────────────────────────

    /// <inheritdoc />
    public ValueTask<TResult?> GetAsync<TState, TResult>(
        Func<IQueryable<OpenIddictAuthorizationRecord>, TState, IQueryable<TResult>> query,
        TState state,
        CancellationToken cancellationToken)
        => throw new NotSupportedException(
            "LINQ-based GetAsync is not supported by the DataGateway store.");

    // ── Getters ────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public ValueTask<string?> GetApplicationIdAsync(OpenIddictAuthorizationRecord authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        return new ValueTask<string?>(authorization.ApplicationId?.ToString());
    }

    /// <inheritdoc />
    public ValueTask<DateTimeOffset?> GetCreationDateAsync(OpenIddictAuthorizationRecord authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        return new ValueTask<DateTimeOffset?>(
            authorization.CreationDate == default ? null : authorization.CreationDate);
    }

    /// <inheritdoc />
    public ValueTask<string?> GetIdAsync(OpenIddictAuthorizationRecord authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        return new ValueTask<string?>(authorization.Id == Guid.Empty ? null : authorization.Id.ToString());
    }

    /// <inheritdoc />
    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(
        OpenIddictAuthorizationRecord authorization,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        // Why: Properties column omitted per Phase 1 design decision.
        return new ValueTask<ImmutableDictionary<string, JsonElement>>(
            ImmutableDictionary<string, JsonElement>.Empty);
    }

    /// <inheritdoc />
    public async ValueTask<ImmutableArray<string>> GetScopesAsync(
        OpenIddictAuthorizationRecord authorization,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        if (authorization.Id == Guid.Empty)
        {
            return ImmutableArray<string>.Empty;
        }

        var command = Query.From<OpenIddictAuthorizationScopeRecord>(DataStoreName, PathName, AuthorizationScopeContainer)
            .Where(r => r.AuthorizationId).Equal(authorization.Id)
            .Build();

        return UnwrapValue(await QueryCurrent<OpenIddictAuthorizationScopeRecord>(command, cancellationToken).ConfigureAwait(false))
            .Select(r => r.Scope)
            .ToImmutableArray();
    }

    /// <inheritdoc />
    public ValueTask<string?> GetStatusAsync(OpenIddictAuthorizationRecord authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        return new ValueTask<string?>(string.IsNullOrEmpty(authorization.Status) ? null : authorization.Status);
    }

    /// <inheritdoc />
    public ValueTask<string?> GetSubjectAsync(OpenIddictAuthorizationRecord authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        return new ValueTask<string?>(authorization.Subject);
    }

    /// <inheritdoc />
    public ValueTask<string?> GetTypeAsync(OpenIddictAuthorizationRecord authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        return new ValueTask<string?>(string.IsNullOrEmpty(authorization.AuthorizationType) ? null : authorization.AuthorizationType);
    }

    // ── Instantiate ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public ValueTask<OpenIddictAuthorizationRecord> InstantiateAsync(CancellationToken cancellationToken)
        => new(new OpenIddictAuthorizationRecord());

    // ── List ────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async IAsyncEnumerable<OpenIddictAuthorizationRecord> ListAsync(
        int? count,
        int? offset,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var queryBuilder = Query.From<OpenIddictAuthorizationRecord>(DataStoreName, PathName, AuthorizationContainer);

        if (offset.HasValue || count.HasValue)
        {
            queryBuilder = queryBuilder.Paging(offset ?? 0, count ?? int.MaxValue);
        }

        foreach (var auth in UnwrapValue(await QueryCurrent<OpenIddictAuthorizationRecord>(queryBuilder.Build(), cancellationToken).ConfigureAwait(false)))
        {
            yield return auth;
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(
        Func<IQueryable<OpenIddictAuthorizationRecord>, TState, IQueryable<TResult>> query,
        TState state,
        CancellationToken cancellationToken)
        => throw new NotSupportedException(
            "LINQ-based ListAsync is not supported by the DataGateway store. Use the paged ListAsync overload.");

    // ── Prune & Revoke ──────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<long> PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
    {
        // Why: Remove ad-hoc authorizations created before the threshold. Hard-delete —
        // authorizations are operational data; no audit history is needed.
        var command = Query.From<OpenIddictAuthorizationRecord>(DataStoreName, PathName, AuthorizationContainer)
            .Where(r => r.CreationDate).LessThan(threshold)
            .Build();

        var candidates = UnwrapValue(await QueryCurrent<OpenIddictAuthorizationRecord>(command, cancellationToken).ConfigureAwait(false)).ToList();
        long count = 0;

        foreach (var auth in candidates)
        {
            // Why: Delete child scopes first, then the parent. Children have no hard FK constraint
            // (AuthorizationId references the logical Id column) but delete order still matters
            // for correctness — a partial child delete with a surviving parent would leave orphaned
            // scopes associated with a deleted authorization.
            var childResult = await DeleteRecord(
                AuthorizationScopeContainer, nameof(OpenIddictAuthorizationScopeRecord.AuthorizationId), auth.Id, cancellationToken).ConfigureAwait(false);
            if (!childResult.IsSuccess)
            {
                OpenIddictStoreLog.DataGatewayDeleteFailed(Logger, childResult.CurrentMessage!);
                continue;
            }

            var r = await DeleteRecord(
                AuthorizationContainer, nameof(OpenIddictAuthorizationRecord.Id), auth.Id, cancellationToken).ConfigureAwait(false);
            if (r.IsSuccess) count++;
        }

        OpenIddictStoreLog.AuthorizationPruned(Logger, count, threshold);
        return count;
    }

    /// <inheritdoc />
    public async ValueTask<long> RevokeAsync(
        string? subject,
        string? client,
        string? status,
        string? type,
        CancellationToken cancellationToken)
    {
        var queryBuilder = Query.From<OpenIddictAuthorizationRecord>(DataStoreName, PathName, AuthorizationContainer);

        if (!string.IsNullOrEmpty(subject))
        {
            queryBuilder = queryBuilder.Where(r => r.Subject).Equal(subject);
        }

        if (!string.IsNullOrEmpty(client) && Guid.TryParse(client, out var clientId))
        {
            queryBuilder = queryBuilder.Where(r => r.ApplicationId).Equal(clientId);
        }

        if (!string.IsNullOrEmpty(status))
        {
            queryBuilder = queryBuilder.Where(r => r.Status).Equal(status);
        }

        if (!string.IsNullOrEmpty(type))
        {
            queryBuilder = queryBuilder.Where(r => r.AuthorizationType).Equal(type);
        }

        var targets = UnwrapValue(await QueryCurrent<OpenIddictAuthorizationRecord>(queryBuilder.Build(), cancellationToken).ConfigureAwait(false)).ToList();
        return await RevokeAuthorizations(targets, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<long> RevokeByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(identifier, out var appId))
        {
            return 0;
        }

        var command = Query.From<OpenIddictAuthorizationRecord>(DataStoreName, PathName, AuthorizationContainer)
            .Where(r => r.ApplicationId).Equal(appId)
            .Build();

        var targets = UnwrapValue(await QueryCurrent<OpenIddictAuthorizationRecord>(command, cancellationToken).ConfigureAwait(false)).ToList();
        return await RevokeAuthorizations(targets, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<long> RevokeBySubjectAsync(string subject, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(subject);

        var command = Query.From<OpenIddictAuthorizationRecord>(DataStoreName, PathName, AuthorizationContainer)
            .Where(r => r.Subject).Equal(subject)
            .Build();

        var targets = UnwrapValue(await QueryCurrent<OpenIddictAuthorizationRecord>(command, cancellationToken).ConfigureAwait(false)).ToList();
        return await RevokeAuthorizations(targets, cancellationToken).ConfigureAwait(false);
    }

    // ── Setters ────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public ValueTask SetApplicationIdAsync(OpenIddictAuthorizationRecord authorization, string? identifier, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        authorization.ApplicationId = Guid.TryParse(identifier, out var id) ? id : null;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetCreationDateAsync(OpenIddictAuthorizationRecord authorization, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        authorization.CreationDate = date ?? DateTimeOffset.UtcNow;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetPropertiesAsync(
        OpenIddictAuthorizationRecord authorization,
        ImmutableDictionary<string, JsonElement> properties,
        CancellationToken cancellationToken)
    {
        // Why: Properties column omitted per Phase 1 design decision. No-op.
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetScopesAsync(
        OpenIddictAuthorizationRecord authorization,
        ImmutableArray<string> scopes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        _pendingScopes.GetOrCreateValue(authorization).Value = scopes;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetStatusAsync(OpenIddictAuthorizationRecord authorization, string? status, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        authorization.Status = status ?? string.Empty;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetSubjectAsync(OpenIddictAuthorizationRecord authorization, string? subject, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        authorization.Subject = subject;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetTypeAsync(OpenIddictAuthorizationRecord authorization, string? type, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        authorization.AuthorizationType = type ?? string.Empty;
        return ValueTask.CompletedTask;
    }

    // ── Private helpers ────────────────────────────────────────────────────────────

    private Task<IGenericResult> InsertScopeSet(
        Guid authorizationId,
        ImmutableArray<string> scopes,
        CancellationToken cancellationToken)
    {
        var rows = scopes
            .Select(s => new OpenIddictAuthorizationScopeRecord
            {
                // Why: each scope row needs a unique clustered-PK RowId. The record left it default
                // (Guid.Empty), so a multi-scope grant (e.g. offline_access + fdw.api) collided on
                // PK_OpenIddictAuthorizationScope. Mint per row (the insert supplies RowId, overriding
                // the table's newsequentialid default).
                AuthorizationId = authorizationId,
                Scope = s,
                CreatedAt = DateTimeOffset.UtcNow
            })
            .ToList();

        return InsertChildSet(AuthorizationScopeContainer, rows, cancellationToken);
    }

    // Why: Update-in-place revoke: set Status="revoked" and ModifiedAt on the existing row.
    // No new version row is inserted; the single row per Id is modified directly.
    private async Task<long> RevokeAuthorizations(
        IReadOnlyList<OpenIddictAuthorizationRecord> targets,
        CancellationToken cancellationToken)
    {
        long count = 0;

        foreach (var auth in targets)
        {
            auth.Status = RevokedStatus;
            auth.ModifiedAt = DateTimeOffset.UtcNow;

            var update = await UpdateRecord(
                AuthorizationContainer, nameof(OpenIddictAuthorizationRecord.Id), auth.Id, auth, cancellationToken).ConfigureAwait(false);
            if (update.IsSuccess) count++;
        }

        return count;
    }
}
