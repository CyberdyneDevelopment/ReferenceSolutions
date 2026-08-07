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
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using Query = Fdw.Commands.Data.DataQuery;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Storage;

/// <summary>
/// DataGateway-backed <see cref="IOpenIddictApplicationStore{TApplication}"/> over <see cref="OpenIddictApplicationRecord"/>.
/// Persists to <c>auth.OpenIddictApplication</c> (parent, version-on-write) and four child tables (Permissions,
/// RedirectUris, PostLogoutRedirectUris, Requirements), each version-on-write via Set*Async.
/// Properties, DisplayNames (locale), Settings, and JsonWebKeySet are omitted per Phase 1 design.
/// LINQ-based overloads (CountAsync&lt;TResult&gt;, GetAsync, ListAsync&lt;TState,TResult&gt;) throw <see cref="NotSupportedException"/>.
/// </summary>
internal sealed class OpenIddictApplicationStore : OpenIddictStoreBase, IOpenIddictApplicationStore<OpenIddictApplicationRecord>
{
    private const string ApplicationContainer = "OpenIddictApplication";
    private const string PermissionContainer = "OpenIddictApplicationPermission";
    private const string RedirectUriContainer = "OpenIddictApplicationRedirectUri";
    private const string PostLogoutRedirectUriContainer = "OpenIddictApplicationPostLogoutRedirectUri";
    private const string RequirementContainer = "OpenIddictApplicationRequirement";

    // Why: Same ConditionalWeakTable staging pattern as ScopeStore. [GenerateMapper] maps every property
    // on the record to a SQL column; staging state must NOT live on the record or the generated INSERT
    // will include a non-existent column and fail. One table per child collection.
    private sealed class PendingChildSets
    {
        public ImmutableArray<string>? Permissions { get; set; }
        public ImmutableArray<string>? RedirectUris { get; set; }
        public ImmutableArray<string>? PostLogoutRedirectUris { get; set; }
        public ImmutableArray<string>? Requirements { get; set; }
    }
    private static readonly ConditionalWeakTable<OpenIddictApplicationRecord, PendingChildSets> _pending = new();

    /// <summary>Initializes a new instance of <see cref="OpenIddictApplicationStore"/>.</summary>
    public OpenIddictApplicationStore(Lazy<IDataGateway> dataGateway, ILogger<OpenIddictApplicationStore>? logger)
        : base(dataGateway, logger)
    {
    }

    // ── Persistence ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask CreateAsync(OpenIddictApplicationRecord application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        OpenIddictStoreLog.ApplicationCreateStarted(Logger, application.ClientId);

        try
        {
            if (application.Id == Guid.Empty)
            {
                application.Id = Guid.NewGuid();
            }

            // Why: RowId is the physical PK — now a DB-managed INT IDENTITY, invisible to the app and not a
            // record property. The INSERT omits it and the DB assigns it; no app-side mint.
            application.IsCurrent = true;
            application.IsDeleted = false;
            application.CreatedAt = DateTimeOffset.UtcNow;
            application.ModifiedAt = DateTimeOffset.UtcNow;

            Unwrap(await InsertVersion(ApplicationContainer, application, cancellationToken).ConfigureAwait(false));

            if (_pending.TryGetValue(application, out var pending))
            {
                Unwrap(await FlushChildSets(application.Id, pending, cancellationToken).ConfigureAwait(false));
                _pending.Remove(application);
            }

            OpenIddictStoreLog.ApplicationCreated(Logger, application.Id, application.ClientId);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                OpenIddictStoreLog.ApplicationCreateFailed(Logger, ex, application.ClientId).Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask UpdateAsync(OpenIddictApplicationRecord application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        OpenIddictStoreLog.ApplicationUpdateStarted(Logger, application.Id);

        try
        {
            // Why: Version-on-write. Supersede the current parent row then insert a fresh version.
            Unwrap(await SupersedeCurrent(
                ApplicationContainer,
                nameof(OpenIddictApplicationRecord.Id),
                application.Id,
                cancellationToken).ConfigureAwait(false));

            application.IsCurrent = true;
            application.ModifiedAt = DateTimeOffset.UtcNow;
            if (application.CreatedAt == default)
            {
                application.CreatedAt = DateTimeOffset.UtcNow;
            }

            // Why: version-on-write inserts a NEW physical row; RowId is a DB-managed INT IDENTITY (invisible,
            // not a record property), so the DB assigns a fresh RowId on this INSERT — no collision with the
            // just-superseded row, no app-side mint. The logical Id is unchanged — it links the versions.
            Unwrap(await InsertVersion(ApplicationContainer, application, cancellationToken).ConfigureAwait(false));

            if (_pending.TryGetValue(application, out var pending))
            {
                Unwrap(await ApplyStagedChildSets(application.Id, pending, cancellationToken).ConfigureAwait(false));
                _pending.Remove(application);
            }

            OpenIddictStoreLog.ApplicationUpdated(Logger, application.Id);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                OpenIddictStoreLog.ApplicationUpdateFailed(Logger, ex, application.Id).Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask DeleteAsync(OpenIddictApplicationRecord application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        try
        {
            Unwrap(await SupersedeCurrentAndDelete(
                ApplicationContainer,
                nameof(OpenIddictApplicationRecord.Id),
                application.Id,
                cancellationToken).ConfigureAwait(false));

            Unwrap(await SupersedeCurrentAndDelete(PermissionContainer, "ApplicationId", application.Id, cancellationToken).ConfigureAwait(false));
            Unwrap(await SupersedeCurrentAndDelete(RedirectUriContainer, "ApplicationId", application.Id, cancellationToken).ConfigureAwait(false));
            Unwrap(await SupersedeCurrentAndDelete(PostLogoutRedirectUriContainer, "ApplicationId", application.Id, cancellationToken).ConfigureAwait(false));
            Unwrap(await SupersedeCurrentAndDelete(RequirementContainer, "ApplicationId", application.Id, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                OpenIddictStoreLog.ApplicationDeleteFailed(Logger, ex, application.Id).Message, ex);
        }
    }

    // ── Count ───────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<long> CountAsync(CancellationToken cancellationToken)
    {
        var command = Query.From<OpenIddictApplicationRecord>(DataStoreName, PathName, ApplicationContainer)
            .Where(r => r.IsCurrent).Equal(true)
            .Where(r => r.IsDeleted).Equal(false)
            .Build();

        return UnwrapValue(await QueryCurrent<OpenIddictApplicationRecord>(command, cancellationToken).ConfigureAwait(false)).LongCount();
    }

    /// <inheritdoc />
    public ValueTask<long> CountAsync<TResult>(
        Func<IQueryable<OpenIddictApplicationRecord>, IQueryable<TResult>> query,
        CancellationToken cancellationToken)
        => throw new NotSupportedException(
            "LINQ-based CountAsync is not supported by the DataGateway store. Use the plain CountAsync overload.");

    // ── Find ────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask<OpenIddictApplicationRecord?> FindByClientIdAsync(
        string identifier,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);

        var command = Query.From<OpenIddictApplicationRecord>(DataStoreName, PathName, ApplicationContainer)
            .Where(r => r.ClientId).Equal(identifier)
            .Where(r => r.IsCurrent).Equal(true)
            .Where(r => r.IsDeleted).Equal(false)
            .Build();

        return UnwrapValue(await QueryCurrent<OpenIddictApplicationRecord>(command, cancellationToken).ConfigureAwait(false))
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public async ValueTask<OpenIddictApplicationRecord?> FindByIdAsync(
        string identifier,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(identifier, out var id))
        {
            return null;
        }

        var command = Query.From<OpenIddictApplicationRecord>(DataStoreName, PathName, ApplicationContainer)
            .Where(r => r.Id).Equal(id)
            .Where(r => r.IsCurrent).Equal(true)
            .Where(r => r.IsDeleted).Equal(false)
            .Build();

        return UnwrapValue(await QueryCurrent<OpenIddictApplicationRecord>(command, cancellationToken).ConfigureAwait(false))
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<OpenIddictApplicationRecord> FindByRedirectUriAsync(
        string uri,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(uri);

        var childCommand = Query.From<OpenIddictApplicationRedirectUriRecord>(DataStoreName, PathName, RedirectUriContainer)
            .Where(r => r.Uri).Equal(uri)
            .Where(r => r.IsCurrent).Equal(true)
            .Where(r => r.IsDeleted).Equal(false)
            .Build();

        var appIds = UnwrapValue(await QueryCurrent<OpenIddictApplicationRedirectUriRecord>(childCommand, cancellationToken).ConfigureAwait(false))
            .Select(r => r.ApplicationId).Distinct().ToList();

        foreach (var appId in appIds)
        {
            var app = await FindByIdAsync(appId.ToString(), cancellationToken).ConfigureAwait(false);
            if (app is not null)
            {
                yield return app;
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<OpenIddictApplicationRecord> FindByPostLogoutRedirectUriAsync(
        string uri,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(uri);

        var childCommand = Query.From<OpenIddictApplicationPostLogoutRedirectUriRecord>(DataStoreName, PathName, PostLogoutRedirectUriContainer)
            .Where(r => r.Uri).Equal(uri)
            .Where(r => r.IsCurrent).Equal(true)
            .Where(r => r.IsDeleted).Equal(false)
            .Build();

        var appIds = UnwrapValue(await QueryCurrent<OpenIddictApplicationPostLogoutRedirectUriRecord>(childCommand, cancellationToken).ConfigureAwait(false))
            .Select(r => r.ApplicationId).Distinct().ToList();

        foreach (var appId in appIds)
        {
            var app = await FindByIdAsync(appId.ToString(), cancellationToken).ConfigureAwait(false);
            if (app is not null)
            {
                yield return app;
            }
        }
    }

    // ── GetAsync (LINQ — not supported) ─────────────────────────────────────────────

    /// <inheritdoc />
    public ValueTask<TResult?> GetAsync<TState, TResult>(
        Func<IQueryable<OpenIddictApplicationRecord>, TState, IQueryable<TResult>> query,
        TState state,
        CancellationToken cancellationToken)
        => throw new NotSupportedException(
            "LINQ-based GetAsync is not supported by the DataGateway store.");

    // ── Getters ────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public ValueTask<string?> GetApplicationTypeAsync(OpenIddictApplicationRecord application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        return new ValueTask<string?>(application.ApplicationType);
    }

    /// <inheritdoc />
    public ValueTask<string?> GetClientIdAsync(OpenIddictApplicationRecord application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        return new ValueTask<string?>(string.IsNullOrEmpty(application.ClientId) ? null : application.ClientId);
    }

    /// <inheritdoc />
    public ValueTask<string?> GetClientSecretAsync(OpenIddictApplicationRecord application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        return new ValueTask<string?>(application.ClientSecretHash);
    }

    /// <inheritdoc />
    public ValueTask<string?> GetClientTypeAsync(OpenIddictApplicationRecord application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        return new ValueTask<string?>(string.IsNullOrEmpty(application.ClientType) ? null : application.ClientType);
    }

    /// <inheritdoc />
    public ValueTask<string?> GetConsentTypeAsync(OpenIddictApplicationRecord application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        return new ValueTask<string?>(application.ConsentType);
    }

    /// <inheritdoc />
    public ValueTask<string?> GetDisplayNameAsync(OpenIddictApplicationRecord application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        return new ValueTask<string?>(application.DisplayName);
    }

    /// <inheritdoc />
    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(
        OpenIddictApplicationRecord application,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        // Why: Locale-specific display names omitted per Phase 1 design (schema has single DisplayName column).
        return new ValueTask<ImmutableDictionary<CultureInfo, string>>(
            ImmutableDictionary<CultureInfo, string>.Empty);
    }

    /// <inheritdoc />
    public ValueTask<string?> GetIdAsync(OpenIddictApplicationRecord application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        return new ValueTask<string?>(application.Id == Guid.Empty ? null : application.Id.ToString());
    }

    /// <inheritdoc />
    public ValueTask<JsonWebKeySet?> GetJsonWebKeySetAsync(OpenIddictApplicationRecord application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        // Why: JsonWebKeySet column omitted per Phase 1 design. RS256 signing keys are managed by the authority, not per-application.
        return new ValueTask<JsonWebKeySet?>((JsonWebKeySet?)null);
    }

    /// <inheritdoc />
    public async ValueTask<ImmutableArray<string>> GetPermissionsAsync(
        OpenIddictApplicationRecord application,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (application.Id == Guid.Empty)
        {
            return ImmutableArray<string>.Empty;
        }

        var command = Query.From<OpenIddictApplicationPermissionRecord>(DataStoreName, PathName, PermissionContainer)
            .Where(r => r.ApplicationId).Equal(application.Id)
            .Where(r => r.IsCurrent).Equal(true)
            .Where(r => r.IsDeleted).Equal(false)
            .Build();

        return UnwrapValue(await QueryCurrent<OpenIddictApplicationPermissionRecord>(command, cancellationToken).ConfigureAwait(false))
            .Select(r => r.Permission)
            .ToImmutableArray();
    }

    /// <inheritdoc />
    public async ValueTask<ImmutableArray<string>> GetRedirectUrisAsync(
        OpenIddictApplicationRecord application,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (application.Id == Guid.Empty)
        {
            return ImmutableArray<string>.Empty;
        }

        var command = Query.From<OpenIddictApplicationRedirectUriRecord>(DataStoreName, PathName, RedirectUriContainer)
            .Where(r => r.ApplicationId).Equal(application.Id)
            .Where(r => r.IsCurrent).Equal(true)
            .Where(r => r.IsDeleted).Equal(false)
            .Build();

        return UnwrapValue(await QueryCurrent<OpenIddictApplicationRedirectUriRecord>(command, cancellationToken).ConfigureAwait(false))
            .Select(r => r.Uri)
            .ToImmutableArray();
    }

    /// <inheritdoc />
    public async ValueTask<ImmutableArray<string>> GetPostLogoutRedirectUrisAsync(
        OpenIddictApplicationRecord application,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (application.Id == Guid.Empty)
        {
            return ImmutableArray<string>.Empty;
        }

        var command = Query.From<OpenIddictApplicationPostLogoutRedirectUriRecord>(DataStoreName, PathName, PostLogoutRedirectUriContainer)
            .Where(r => r.ApplicationId).Equal(application.Id)
            .Where(r => r.IsCurrent).Equal(true)
            .Where(r => r.IsDeleted).Equal(false)
            .Build();

        return UnwrapValue(await QueryCurrent<OpenIddictApplicationPostLogoutRedirectUriRecord>(command, cancellationToken).ConfigureAwait(false))
            .Select(r => r.Uri)
            .ToImmutableArray();
    }

    /// <inheritdoc />
    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(
        OpenIddictApplicationRecord application,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        // Why: Properties column omitted per Phase 1 design decision. Returns empty dictionary.
        return new ValueTask<ImmutableDictionary<string, JsonElement>>(
            ImmutableDictionary<string, JsonElement>.Empty);
    }

    /// <inheritdoc />
    public async ValueTask<ImmutableArray<string>> GetRequirementsAsync(
        OpenIddictApplicationRecord application,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (application.Id == Guid.Empty)
        {
            return ImmutableArray<string>.Empty;
        }

        var command = Query.From<OpenIddictApplicationRequirementRecord>(DataStoreName, PathName, RequirementContainer)
            .Where(r => r.ApplicationId).Equal(application.Id)
            .Where(r => r.IsCurrent).Equal(true)
            .Where(r => r.IsDeleted).Equal(false)
            .Build();

        return UnwrapValue(await QueryCurrent<OpenIddictApplicationRequirementRecord>(command, cancellationToken).ConfigureAwait(false))
            .Select(r => r.Requirement)
            .ToImmutableArray();
    }

    /// <inheritdoc />
    public ValueTask<ImmutableDictionary<string, string>> GetSettingsAsync(
        OpenIddictApplicationRecord application,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        // Why: Settings column omitted per Phase 1 design. No per-application settings in schema.
        return new ValueTask<ImmutableDictionary<string, string>>(
            ImmutableDictionary<string, string>.Empty);
    }

    // ── Instantiate ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public ValueTask<OpenIddictApplicationRecord> InstantiateAsync(CancellationToken cancellationToken)
        => new(new OpenIddictApplicationRecord());

    // ── List ────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async IAsyncEnumerable<OpenIddictApplicationRecord> ListAsync(
        int? count,
        int? offset,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var queryBuilder = Query.From<OpenIddictApplicationRecord>(DataStoreName, PathName, ApplicationContainer)
            .Where(r => r.IsCurrent).Equal(true)
            .Where(r => r.IsDeleted).Equal(false);

        if (offset.HasValue || count.HasValue)
        {
            queryBuilder = queryBuilder.Paging(offset ?? 0, count ?? int.MaxValue);
        }

        foreach (var app in UnwrapValue(await QueryCurrent<OpenIddictApplicationRecord>(queryBuilder.Build(), cancellationToken).ConfigureAwait(false)))
        {
            yield return app;
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(
        Func<IQueryable<OpenIddictApplicationRecord>, TState, IQueryable<TResult>> query,
        TState state,
        CancellationToken cancellationToken)
        => throw new NotSupportedException(
            "LINQ-based ListAsync is not supported by the DataGateway store. Use the paged ListAsync overload.");

    // ── Setters ────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public ValueTask SetApplicationTypeAsync(OpenIddictApplicationRecord application, string? type, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        application.ApplicationType = type;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetClientIdAsync(OpenIddictApplicationRecord application, string? identifier, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        application.ClientId = identifier ?? string.Empty;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetClientSecretAsync(OpenIddictApplicationRecord application, string? secret, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        application.ClientSecretHash = secret;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetClientTypeAsync(OpenIddictApplicationRecord application, string? type, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        application.ClientType = type ?? string.Empty;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetConsentTypeAsync(OpenIddictApplicationRecord application, string? type, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        application.ConsentType = type;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetDisplayNameAsync(OpenIddictApplicationRecord application, string? name, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        application.DisplayName = name;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetDisplayNamesAsync(
        OpenIddictApplicationRecord application,
        ImmutableDictionary<CultureInfo, string> names,
        CancellationToken cancellationToken)
    {
        // Why: Locale-specific display names omitted per Phase 1 design. No-op.
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetJsonWebKeySetAsync(OpenIddictApplicationRecord application, JsonWebKeySet? set, CancellationToken cancellationToken)
    {
        // Why: JsonWebKeySet column omitted per Phase 1 design. No-op.
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetPermissionsAsync(
        OpenIddictApplicationRecord application,
        ImmutableArray<string> permissions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        _pending.GetOrCreateValue(application).Permissions = permissions;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetRedirectUrisAsync(
        OpenIddictApplicationRecord application,
        ImmutableArray<string> uris,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        _pending.GetOrCreateValue(application).RedirectUris = uris;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetPostLogoutRedirectUrisAsync(
        OpenIddictApplicationRecord application,
        ImmutableArray<string> uris,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        _pending.GetOrCreateValue(application).PostLogoutRedirectUris = uris;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetPropertiesAsync(
        OpenIddictApplicationRecord application,
        ImmutableDictionary<string, JsonElement> properties,
        CancellationToken cancellationToken)
    {
        // Why: Properties column omitted per Phase 1 design decision. No-op.
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetRequirementsAsync(
        OpenIddictApplicationRecord application,
        ImmutableArray<string> requirements,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        _pending.GetOrCreateValue(application).Requirements = requirements;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetSettingsAsync(
        OpenIddictApplicationRecord application,
        ImmutableDictionary<string, string> settings,
        CancellationToken cancellationToken)
    {
        // Why: Settings column omitted per Phase 1 design. No-op.
        return ValueTask.CompletedTask;
    }

    // ── Private helpers ────────────────────────────────────────────────────────────

    // Inserts all staged child sets for a Create (no supersede).
    private async Task<IGenericResult> FlushChildSets(Guid appId, PendingChildSets pending, CancellationToken ct)
    {
        if (pending.Permissions.HasValue)
        {
            var r = await InsertChildSet(PermissionContainer,
                pending.Permissions.Value.Select(p => new OpenIddictApplicationPermissionRecord
                {
                    ApplicationId = appId, Permission = p, IsCurrent = true, IsDeleted = false, CreatedAt = DateTimeOffset.UtcNow
                }).ToList(), ct).ConfigureAwait(false);
            if (!r.IsSuccess) return r;
        }

        if (pending.RedirectUris.HasValue)
        {
            var r = await InsertChildSet(RedirectUriContainer,
                pending.RedirectUris.Value.Select(u => new OpenIddictApplicationRedirectUriRecord
                {
                    ApplicationId = appId, Uri = u, IsCurrent = true, IsDeleted = false, CreatedAt = DateTimeOffset.UtcNow
                }).ToList(), ct).ConfigureAwait(false);
            if (!r.IsSuccess) return r;
        }

        if (pending.PostLogoutRedirectUris.HasValue)
        {
            var r = await InsertChildSet(PostLogoutRedirectUriContainer,
                pending.PostLogoutRedirectUris.Value.Select(u => new OpenIddictApplicationPostLogoutRedirectUriRecord
                {
                    ApplicationId = appId, Uri = u, IsCurrent = true, IsDeleted = false, CreatedAt = DateTimeOffset.UtcNow
                }).ToList(), ct).ConfigureAwait(false);
            if (!r.IsSuccess) return r;
        }

        if (pending.Requirements.HasValue)
        {
            var r = await InsertChildSet(RequirementContainer,
                pending.Requirements.Value.Select(req => new OpenIddictApplicationRequirementRecord
                {
                    ApplicationId = appId, Requirement = req, IsCurrent = true, IsDeleted = false, CreatedAt = DateTimeOffset.UtcNow
                }).ToList(), ct).ConfigureAwait(false);
            if (!r.IsSuccess) return r;
        }

        return GenericResult.Success();
    }

    // Supersedes existing child set rows then inserts new rows for an Update.
    private async Task<IGenericResult> ApplyStagedChildSets(Guid appId, PendingChildSets pending, CancellationToken ct)
    {
        if (pending.Permissions.HasValue)
        {
            var r = await SupersedeCurrent(PermissionContainer, "ApplicationId", appId, ct).ConfigureAwait(false);
            if (!r.IsSuccess) return r;
            r = await InsertChildSet(PermissionContainer,
                pending.Permissions.Value.Select(p => new OpenIddictApplicationPermissionRecord
                {
                    ApplicationId = appId, Permission = p, IsCurrent = true, IsDeleted = false, CreatedAt = DateTimeOffset.UtcNow
                }).ToList(), ct).ConfigureAwait(false);
            if (!r.IsSuccess) return r;
        }

        if (pending.RedirectUris.HasValue)
        {
            var r = await SupersedeCurrent(RedirectUriContainer, "ApplicationId", appId, ct).ConfigureAwait(false);
            if (!r.IsSuccess) return r;
            r = await InsertChildSet(RedirectUriContainer,
                pending.RedirectUris.Value.Select(u => new OpenIddictApplicationRedirectUriRecord
                {
                    ApplicationId = appId, Uri = u, IsCurrent = true, IsDeleted = false, CreatedAt = DateTimeOffset.UtcNow
                }).ToList(), ct).ConfigureAwait(false);
            if (!r.IsSuccess) return r;
        }

        if (pending.PostLogoutRedirectUris.HasValue)
        {
            var r = await SupersedeCurrent(PostLogoutRedirectUriContainer, "ApplicationId", appId, ct).ConfigureAwait(false);
            if (!r.IsSuccess) return r;
            r = await InsertChildSet(PostLogoutRedirectUriContainer,
                pending.PostLogoutRedirectUris.Value.Select(u => new OpenIddictApplicationPostLogoutRedirectUriRecord
                {
                    ApplicationId = appId, Uri = u, IsCurrent = true, IsDeleted = false, CreatedAt = DateTimeOffset.UtcNow
                }).ToList(), ct).ConfigureAwait(false);
            if (!r.IsSuccess) return r;
        }

        if (pending.Requirements.HasValue)
        {
            var r = await SupersedeCurrent(RequirementContainer, "ApplicationId", appId, ct).ConfigureAwait(false);
            if (!r.IsSuccess) return r;
            r = await InsertChildSet(RequirementContainer,
                pending.Requirements.Value.Select(req => new OpenIddictApplicationRequirementRecord
                {
                    ApplicationId = appId, Requirement = req, IsCurrent = true, IsDeleted = false, CreatedAt = DateTimeOffset.UtcNow
                }).ToList(), ct).ConfigureAwait(false);
            if (!r.IsSuccess) return r;
        }

        return GenericResult.Success();
    }
}
