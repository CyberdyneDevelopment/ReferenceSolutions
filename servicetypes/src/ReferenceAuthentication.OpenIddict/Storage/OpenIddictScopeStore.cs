using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
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
/// DataGateway-backed <see cref="IOpenIddictScopeStore{TScope}"/> over <see cref="OpenIddictScopeRecord"/>.
/// Persists to <c>auth.OpenIddictScope</c> (parent, version-on-write) and
/// <c>auth.OpenIddictScopeResource</c> (child, version-on-write via SetResourcesAsync).
/// Properties are omitted per Phase 1 design (GetPropertiesAsync returns empty; SetPropertiesAsync is a no-op).
/// LINQ-based overloads (CountAsync&lt;TResult&gt;, GetAsync, ListAsync&lt;TState,TResult&gt;)
/// are not supported and throw <see cref="NotSupportedException"/>.
/// </summary>
internal sealed class OpenIddictScopeStore : OpenIddictStoreBase, IOpenIddictScopeStore<OpenIddictScopeRecord>
{
    private const string ScopeContainer = "OpenIddictScope";
    private const string ScopeResourceContainer = "OpenIddictScopeResource";

    // Why: [GenerateMapper] maps every property on the record to a SQL column. Staging state
    // (resources set before CreateAsync is called) must NOT live on the record itself or the
    // generated INSERT will include a non-existent column and fail. ConditionalWeakTable associates
    // state with each record instance without modifying it; GC cleans up automatically when the
    // scope object is collected.
    private sealed class PendingResources
    {
        public ImmutableArray<string>? Value { get; set; }
    }
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<OpenIddictScopeRecord, PendingResources> _pendingResources = new();

    /// <summary>Initializes a new instance of <see cref="OpenIddictScopeStore"/>.</summary>
    public OpenIddictScopeStore(Lazy<IDataGateway> dataGateway, IAuthenticationContextAccessor authContext, ILogger<OpenIddictScopeStore>? logger)
        : base(dataGateway, authContext, logger)
    {
    }

    // ── Persistence ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask CreateAsync(OpenIddictScopeRecord scope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        OpenIddictStoreLog.ScopeCreateStarted(Logger, scope.Name);

        try
        {
            if (scope.Id == Guid.Empty)
            {
                scope.Id = Guid.NewGuid();
            }

            scope.IsCurrent = true;
            scope.IsDeleted = false;
            scope.CreatedAt = DateTimeOffset.UtcNow;
            scope.ModifiedAt = DateTimeOffset.UtcNow;

            Unwrap(await InsertVersion(ScopeContainer, scope, cancellationToken).ConfigureAwait(false));

            // Flush pending resources staged by SetResourcesAsync before CreateAsync was called.
            if (_pendingResources.TryGetValue(scope, out var pending) && pending.Value.HasValue)
            {
                Unwrap(await InsertResourceSet(scope.Id, pending.Value.Value, cancellationToken)
                    .ConfigureAwait(false));
                _pendingResources.Remove(scope);
            }

            OpenIddictStoreLog.ScopeCreated(Logger, scope.Id, scope.Name);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                OpenIddictStoreLog.ScopeCreateFailed(Logger, ex, scope.Name).Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask UpdateAsync(OpenIddictScopeRecord scope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        OpenIddictStoreLog.ScopeUpdateStarted(Logger, scope.Id);

        try
        {
            // Why: Version-on-write. Supersede the current parent row (IsCurrent=1→0) then insert a fresh version.
            Unwrap(await SupersedeCurrent(
                ScopeContainer,
                nameof(OpenIddictScopeRecord.Id),
                scope.Id,
                cancellationToken).ConfigureAwait(false));

            scope.IsCurrent = true;
            scope.ModifiedAt = DateTimeOffset.UtcNow;
            if (scope.CreatedAt == default)
            {
                scope.CreatedAt = DateTimeOffset.UtcNow;
            }

            Unwrap(await InsertVersion(ScopeContainer, scope, cancellationToken).ConfigureAwait(false));

            // Apply staged resource changes if SetResourcesAsync was called before UpdateAsync.
            if (_pendingResources.TryGetValue(scope, out var pending) && pending.Value.HasValue)
            {
                Unwrap(await SupersedeCurrent(
                    ScopeResourceContainer,
                    nameof(OpenIddictScopeResourceRecord.ScopeId),
                    scope.Id,
                    cancellationToken).ConfigureAwait(false));

                Unwrap(await InsertResourceSet(scope.Id, pending.Value.Value, cancellationToken)
                    .ConfigureAwait(false));

                _pendingResources.Remove(scope);
            }

            OpenIddictStoreLog.ScopeUpdated(Logger, scope.Id);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                OpenIddictStoreLog.ScopeUpdateFailed(Logger, ex, scope.Id).Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask DeleteAsync(OpenIddictScopeRecord scope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);

        try
        {
            Unwrap(await SupersedeCurrentAndDelete(
                ScopeContainer,
                nameof(OpenIddictScopeRecord.Id),
                scope.Id,
                cancellationToken).ConfigureAwait(false));

            Unwrap(await SupersedeCurrentAndDelete(
                ScopeResourceContainer,
                nameof(OpenIddictScopeResourceRecord.ScopeId),
                scope.Id,
                cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                OpenIddictStoreLog.ScopeDeleteFailed(Logger, ex, scope.Id).Message, ex);
        }
    }

    // ── Count ───────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<long> CountAsync(CancellationToken cancellationToken)
    {
        var command = Query.From<OpenIddictScopeRecord>(DataStoreName, PathName, ScopeContainer)
            .Where(r => r.IsCurrent).Equal(true)
            .Where(r => r.IsDeleted).Equal(false)
            .Build();

        var result = await QueryCurrent<OpenIddictScopeRecord>(command, cancellationToken).ConfigureAwait(false);
        return UnwrapValue(result).LongCount();
    }

    /// <inheritdoc />
    public ValueTask<long> CountAsync<TResult>(
        Func<IQueryable<OpenIddictScopeRecord>, IQueryable<TResult>> query,
        CancellationToken cancellationToken)
        => throw new NotSupportedException(
            "LINQ-based CountAsync is not supported by the DataGateway store. Use the plain CountAsync overload.");

    // ── Find ────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<OpenIddictScopeRecord?> FindByIdAsync(
        string identifier,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(identifier, out var id))
        {
            return null;
        }

        OpenIddictStoreLog.ScopeFindByIdStarted(Logger, id);

        try
        {
            var command = Query.From<OpenIddictScopeRecord>(DataStoreName, PathName, ScopeContainer)
                .Where(r => r.Id).Equal(id)
                .Where(r => r.IsCurrent).Equal(true)
                .Where(r => r.IsDeleted).Equal(false)
                .Build();

            return UnwrapValue(await QueryCurrent<OpenIddictScopeRecord>(command, cancellationToken).ConfigureAwait(false))
                .FirstOrDefault();
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                OpenIddictStoreLog.ScopeQueryFailed(Logger, ex, id).Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<OpenIddictScopeRecord?> FindByNameAsync(
        string name,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        OpenIddictStoreLog.ScopeFindByNameStarted(Logger, name);

        try
        {
            var command = Query.From<OpenIddictScopeRecord>(DataStoreName, PathName, ScopeContainer)
                .Where(r => r.Name).Equal(name)
                .Where(r => r.IsCurrent).Equal(true)
                .Where(r => r.IsDeleted).Equal(false)
                .Build();

            return UnwrapValue(await QueryCurrent<OpenIddictScopeRecord>(command, cancellationToken).ConfigureAwait(false))
                .FirstOrDefault();
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                OpenIddictStoreLog.ScopeQueryByNameFailed(Logger, ex, name).Message, ex);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<OpenIddictScopeRecord> FindByNamesAsync(
        ImmutableArray<string> names,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Why: Small sets (typically 1-3 names). N+1 is fine for Phase 1.
        foreach (var name in names)
        {
            var scope = await FindByNameAsync(name, cancellationToken).ConfigureAwait(false);
            if (scope is not null)
            {
                yield return scope;
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<OpenIddictScopeRecord> FindByResourceAsync(
        string resource,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(resource);

        var resourceCommand = Query.From<OpenIddictScopeResourceRecord>(DataStoreName, PathName, ScopeResourceContainer)
            .Where(r => r.Resource).Equal(resource)
            .Where(r => r.IsCurrent).Equal(true)
            .Where(r => r.IsDeleted).Equal(false)
            .Build();

        var resourceResult = await QueryCurrent<OpenIddictScopeResourceRecord>(resourceCommand, cancellationToken).ConfigureAwait(false);
        var scopeIds = UnwrapValue(resourceResult).Select(r => r.ScopeId).Distinct().ToList();

        foreach (var scopeId in scopeIds)
        {
            var scope = await FindByIdAsync(scopeId.ToString(), cancellationToken).ConfigureAwait(false);
            if (scope is not null)
            {
                yield return scope;
            }
        }
    }

    // ── GetAsync (LINQ — not supported) ─────────────────────────────────────────────

    /// <inheritdoc />
    public ValueTask<TResult?> GetAsync<TState, TResult>(
        Func<IQueryable<OpenIddictScopeRecord>, TState, IQueryable<TResult>> query,
        TState state,
        CancellationToken cancellationToken)
        => throw new NotSupportedException(
            "LINQ-based GetAsync is not supported by the DataGateway store.");

    // ── Getters ────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public ValueTask<string?> GetDescriptionAsync(
        OpenIddictScopeRecord scope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return new ValueTask<string?>(scope.Description);
    }

    /// <inheritdoc />
    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDescriptionsAsync(
        OpenIddictScopeRecord scope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        // Why: Locale-specific descriptions omitted per Phase 1 design (schema has single Description column).
        return new ValueTask<ImmutableDictionary<CultureInfo, string>>(
            ImmutableDictionary<CultureInfo, string>.Empty);
    }

    /// <inheritdoc />
    public ValueTask<string?> GetDisplayNameAsync(
        OpenIddictScopeRecord scope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return new ValueTask<string?>(scope.DisplayName);
    }

    /// <inheritdoc />
    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(
        OpenIddictScopeRecord scope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        // Why: Locale-specific display names omitted per Phase 1 design (schema has single DisplayName column).
        return new ValueTask<ImmutableDictionary<CultureInfo, string>>(
            ImmutableDictionary<CultureInfo, string>.Empty);
    }

    /// <inheritdoc />
    public ValueTask<string?> GetIdAsync(
        OpenIddictScopeRecord scope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return new ValueTask<string?>(scope.Id == Guid.Empty ? null : scope.Id.ToString());
    }

    /// <inheritdoc />
    public ValueTask<string?> GetNameAsync(
        OpenIddictScopeRecord scope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return new ValueTask<string?>(
            string.IsNullOrEmpty(scope.Name) ? null : scope.Name);
    }

    /// <inheritdoc />
    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(
        OpenIddictScopeRecord scope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        // Why: Properties column omitted per Phase 1 design decision. Returns empty dictionary.
        return new ValueTask<ImmutableDictionary<string, JsonElement>>(
            ImmutableDictionary<string, JsonElement>.Empty);
    }

    /// <inheritdoc />
    public async ValueTask<ImmutableArray<string>> GetResourcesAsync(
        OpenIddictScopeRecord scope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        OpenIddictStoreLog.ScopeGetResourcesStarted(Logger, scope.Id);

        try
        {
            if (scope.Id == Guid.Empty)
            {
                return ImmutableArray<string>.Empty;
            }

            var command = Query.From<OpenIddictScopeResourceRecord>(DataStoreName, PathName, ScopeResourceContainer)
                .Where(r => r.ScopeId).Equal(scope.Id)
                .Where(r => r.IsCurrent).Equal(true)
                .Where(r => r.IsDeleted).Equal(false)
                .Build();

            var result = await QueryCurrent<OpenIddictScopeResourceRecord>(command, cancellationToken).ConfigureAwait(false);
            var resources = UnwrapValue(result)
                .Select(r => r.Resource)
                .ToImmutableArray();

            OpenIddictStoreLog.ScopeGetResourcesComplete(Logger, resources.Length, scope.Id);
            return resources;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                OpenIddictStoreLog.ScopeGetResourcesFailed(Logger, ex, scope.Id).Message, ex);
        }
    }

    // ── Instantiate ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public ValueTask<OpenIddictScopeRecord> InstantiateAsync(CancellationToken cancellationToken)
        => new(new OpenIddictScopeRecord());

    // ── List ────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async IAsyncEnumerable<OpenIddictScopeRecord> ListAsync(
        int? count,
        int? offset,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var queryBuilder = Query.From<OpenIddictScopeRecord>(DataStoreName, PathName, ScopeContainer)
            .Where(r => r.IsCurrent).Equal(true)
            .Where(r => r.IsDeleted).Equal(false);

        if (offset.HasValue || count.HasValue)
        {
            queryBuilder = queryBuilder.Paging(offset ?? 0, count ?? int.MaxValue);
        }

        var result = await QueryCurrent<OpenIddictScopeRecord>(queryBuilder.Build(), cancellationToken).ConfigureAwait(false);

        foreach (var scope in UnwrapValue(result))
        {
            yield return scope;
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(
        Func<IQueryable<OpenIddictScopeRecord>, TState, IQueryable<TResult>> query,
        TState state,
        CancellationToken cancellationToken)
        => throw new NotSupportedException(
            "LINQ-based ListAsync is not supported by the DataGateway store. Use the paged ListAsync overload.");

    // ── Setters ────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public ValueTask SetDescriptionAsync(
        OpenIddictScopeRecord scope,
        string? description,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        scope.Description = description;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetDescriptionsAsync(
        OpenIddictScopeRecord scope,
        ImmutableDictionary<CultureInfo, string> descriptions,
        CancellationToken cancellationToken)
    {
        // Why: Locale-specific descriptions omitted per Phase 1 design. No-op.
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetDisplayNameAsync(
        OpenIddictScopeRecord scope,
        string? displayName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        scope.DisplayName = displayName;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetDisplayNamesAsync(
        OpenIddictScopeRecord scope,
        ImmutableDictionary<CultureInfo, string> displayNames,
        CancellationToken cancellationToken)
    {
        // Why: Locale-specific display names omitted per Phase 1 design. No-op.
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetNameAsync(
        OpenIddictScopeRecord scope,
        string? name,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        scope.Name = name ?? string.Empty;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetPropertiesAsync(
        OpenIddictScopeRecord scope,
        ImmutableDictionary<string, JsonElement> properties,
        CancellationToken cancellationToken)
    {
        // Why: Properties column omitted per Phase 1 design decision. No-op.
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetResourcesAsync(
        OpenIddictScopeRecord scope,
        ImmutableArray<string> resources,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        // Why: Stage the resource set for CreateAsync/UpdateAsync to write to auth.OpenIddictScopeResource.
        // Stored in ConditionalWeakTable (not on the record) so the POCO mapper's generated INSERT
        // does not include a non-existent DB column.
        var pending = _pendingResources.GetOrCreateValue(scope);
        pending.Value = resources;
        return ValueTask.CompletedTask;
    }

    // ── Private helpers ────────────────────────────────────────────────────────────

    private Task<IGenericResult> InsertResourceSet(
        Guid scopeId,
        ImmutableArray<string> resources,
        CancellationToken cancellationToken)
    {
        var rows = resources
            .Select(r => new OpenIddictScopeResourceRecord
            {
                ScopeId = scopeId,
                Resource = r,
                IsCurrent = true,
                IsDeleted = false,
                CreatedAt = DateTimeOffset.UtcNow
            })
            .ToList();

        return InsertChildSet(ScopeResourceContainer, rows, cancellationToken);
    }
}
