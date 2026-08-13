using Fdw.Services.Authentication.Abstractions.Security;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using CmdBuilders = Fdw.Commands.Data.Extensions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Results;
using ReferenceAuthentication.OpenIddict.Storage;
using ReferenceAuthentication.OpenIddict.Storage.Models;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.OpenIddict.Tests;

/// <summary>
/// Real-DB round-trip gate proof for OpenIddictAuthorizationStore.
/// Uses the REAL FDW MsSql DataGateway against AuthDb on the configured MSSQL_TEST_SERVER (no mocks on the DB path).
/// Skipped automatically when MSSQL_SA_PASSWORD is not set.
/// Proves: Create (with scopes) → GetScopes reads back → SetScopes + Update replaces the scope set
///   in-place (old rows deleted, new rows inserted — NO orphan scope rows survive).
/// Gate: exactly 1 authorization row per Id; exactly N scope rows (matching the current set).
/// Isolation: try/finally hard-DELETEs all test rows so AuthDb is left exactly as found.
/// </summary>
public sealed class OpenIddictAuthorizationStoreIntegrationTests
{
    // Why: the target server is deployment-specific and this file is published publicly, so it
    // comes from the environment beside the credential rather than being committed. It gates the
    // same skip as the password: an unset target means these integration tests cannot mean
    // anything, so they skip rather than build a half-formed connection string and fail obscurely.
    private static readonly string? ServerAddress = Environment.GetEnvironmentVariable("MSSQL_TEST_SERVER");
    private const string DatabaseName = "AuthDb";
    private const string LoginName = "sa";
    private const string EnvVarName = "MSSQL_SA_PASSWORD";

    private static string? GetPassword()
    {
        if (string.IsNullOrEmpty(ServerAddress))
            return null;

        var value = Environment.GetEnvironmentVariable(EnvVarName);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static (OpenIddictAuthorizationStore store, MsSqlConnection connection, DirectDataGateway gateway) BuildStore(string password)
    {
        var connectionString = $"Server={ServerAddress},1433;Database={DatabaseName};User Id={LoginName};Password={password};TrustServerCertificate=True;Encrypt=True";
        var msSqlConnection = new MsSqlConnection(
            NullLogger<MsSqlConnection>.Instance,
            new MsSqlConnectionConfiguration(),
            connectionString);

        // Why: Id is included in INSERT (IsSystemProvided=false) because CreateAsync assigns a specific
        // Guid in code (authorization.Id = Guid.NewGuid()). The DB DEFAULT is a fallback only.
        var authContainer = new TestContainer("auth", "OpenIddictAuthorization", new[]
        {
            ("Id",                false),  // natural PK, assigned in code before INSERT
            ("ApplicationId",     false),
            ("Subject",           false),
            ("Status",            false),
            ("AuthorizationType", false),
            ("CreationDate",      false),
            ("CreatedAt",         false),
            ("ModifiedAt",        false)
        });

        // Why: RowId kept as system-provided auto PK; IsCurrent/IsDeleted removed.
        var scopeContainer = new TestContainer("auth", "OpenIddictAuthorizationScope", new[]
        {
            ("RowId",            true),   // auto-generated PK, excluded from INSERT
            ("AuthorizationId",  false),
            ("Scope",            false),
            ("CreatedAt",        false)
        });

        var containers = new Dictionary<string, IDataContainer>(StringComparer.Ordinal)
        {
            ["OpenIddictAuthorization"] = authContainer,
            ["OpenIddictAuthorizationScope"] = scopeContainer
        };

        var gateway = new DirectDataGateway(msSqlConnection, containers);
        var store = new OpenIddictAuthorizationStore(
            new Lazy<IDataGateway>(() => gateway),
            new AuthenticationContextAccessor(),
            NullLogger<OpenIddictAuthorizationStore>.Instance);

        return (store, msSqlConnection, gateway);
    }

    /// <summary>
    /// Gate proof: Create authorization with 2 scopes → GetScopesAsync reads them back (2 rows) →
    /// SetScopesAsync + UpdateAsync replaces old scope set with 1 new scope →
    /// verify via DataGateway query that EXACTLY 1 scope row exists (old rows deleted, not superseded).
    /// Also verifies EXACTLY 1 authorization row (no version accumulation on parent).
    /// After test: SELECT COUNT(*) = 0 for all test rows.
    /// </summary>
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Integration")]
    public async Task CreateAuthorization_GetScopes_SetScopes_VerifiesSingleRow()
    {
        var password = GetPassword();
        if (password is null)
        {
            return;
        }

        var ct = TestContext.Current.CancellationToken;
        var (store, connection, gateway) = BuildStore(password);
        using var _ = connection;

        var subject = $"test.user.{Guid.NewGuid():N}";
        var authId = Guid.Empty;

        try
        {
            // ── Step 1: Create authorization with 2 scopes ────────────────────────
            var auth = await store.InstantiateAsync(ct);
            await store.SetSubjectAsync(auth, subject, ct);
            await store.SetStatusAsync(auth, "valid", ct);
            await store.SetTypeAsync(auth, "permanent", ct);
            await store.SetScopesAsync(auth, ImmutableArray.Create("openid", "fdw.api"), ct);

            await store.CreateAsync(auth, ct);

            auth.Id.ShouldNotBe(Guid.Empty);
            authId = auth.Id;

            // ── Step 2: Gate — 1 auth row, 2 scope rows after Create ──────────────
            var authRowsAfterCreate = await QueryAllAuthRows(authId, gateway, ct);
            authRowsAfterCreate.Count.ShouldBe(1, "Exactly 1 authorization row should exist after Create.");

            var scopeRowsAfterCreate = await QueryAllScopeRows(authId, gateway, ct);
            scopeRowsAfterCreate.Count.ShouldBe(2, "Exactly 2 scope rows should exist after Create.");

            // ── Step 3: GetScopesAsync reads back the 2 scopes ────────────────────
            var scopes = await store.GetScopesAsync(auth, ct);
            scopes.Length.ShouldBe(2);
            scopes.ShouldContain("openid");
            scopes.ShouldContain("fdw.api");

            // ── Step 4: SetScopesAsync + UpdateAsync replaces the scope set ────────
            await store.SetScopesAsync(auth, ImmutableArray.Create("offline_access"), ct);
            await store.UpdateAsync(auth, ct);

            // ── Step 5: GetScopesAsync reflects the new single scope ───────────────
            var scopesAfterUpdate = await store.GetScopesAsync(auth, ct);
            scopesAfterUpdate.Length.ShouldBe(1);
            scopesAfterUpdate.ShouldContain("offline_access");

            // ── Step 6: THE GATE CHECK — 1 auth row, 1 scope row, 0 orphans ────────
            var authRowsAfterUpdate = await QueryAllAuthRows(authId, gateway, ct);
            authRowsAfterUpdate.Count.ShouldBe(1,
                $"Exactly 1 authorization row should exist after Update (no version rows); got {authRowsAfterUpdate.Count}.");

            var allScopeRows = await QueryAllScopeRows(authId, gateway, ct);
            allScopeRows.Count.ShouldBe(1,
                $"Exactly 1 scope row should exist after Update (old rows deleted, not superseded); got {allScopeRows.Count}. " +
                $"Rows: [{string.Join(", ", allScopeRows.Select(r => $"Scope={r.Scope}"))}]");
            allScopeRows[0].Scope.ShouldBe("offline_access");
        }
        finally
        {
            // Hard-DELETE all version rows for this test authorization.
            if (authId != Guid.Empty)
            {
                await HardDeleteAuthorizationRows(authId, gateway, ct);
            }

            // Verify 0 rows remain.
            var remaining = await CountTestRows(authId, gateway, ct);
            remaining.ShouldBe(0,
                $"AuthDb should have 0 test rows after teardown; found {remaining} orphan(s).");
        }
    }

    private static async Task<IReadOnlyList<OpenIddictAuthorizationRecord>> QueryAllAuthRows(
        Guid authId, DirectDataGateway gateway, CancellationToken ct)
    {
        var command = DataQuery.From<OpenIddictAuthorizationRecord>("AuthDb", "auth", "OpenIddictAuthorization")
            .Where(r => r.Id).Equal(authId)
            .Build();

        var result = await gateway.Execute<IEnumerable<OpenIddictAuthorizationRecord>>(command, ct)
            .ConfigureAwait(false);
        result.IsSuccess.ShouldBeTrue("authorization verification query should succeed");
        return (result.Value ?? Enumerable.Empty<OpenIddictAuthorizationRecord>()).ToList();
    }

    private static async Task<IReadOnlyList<OpenIddictAuthorizationScopeRecord>> QueryAllScopeRows(
        Guid authId, DirectDataGateway gateway, CancellationToken ct)
    {
        var command = DataQuery.From<OpenIddictAuthorizationScopeRecord>("AuthDb", "auth", "OpenIddictAuthorizationScope")
            .Where(r => r.AuthorizationId).Equal(authId)
            .Build();

        var result = await gateway.Execute<IEnumerable<OpenIddictAuthorizationScopeRecord>>(command, ct)
            .ConfigureAwait(false);
        result.IsSuccess.ShouldBeTrue("scope verification query should succeed");
        return (result.Value ?? Enumerable.Empty<OpenIddictAuthorizationScopeRecord>()).ToList();
    }

    private static async Task<int> CountTestRows(Guid authId, DirectDataGateway gateway, CancellationToken ct)
    {
        if (authId == Guid.Empty) return 0;

        var authCmd = DataQuery.From<OpenIddictAuthorizationRecord>("AuthDb", "auth", "OpenIddictAuthorization")
            .Where(r => r.Id).Equal(authId)
            .Build();
        var scopeCmd = DataQuery.From<OpenIddictAuthorizationScopeRecord>("AuthDb", "auth", "OpenIddictAuthorizationScope")
            .Where(r => r.AuthorizationId).Equal(authId)
            .Build();

        var authResult = await gateway.Execute<IEnumerable<OpenIddictAuthorizationRecord>>(authCmd, ct).ConfigureAwait(false);
        var scopeResult = await gateway.Execute<IEnumerable<OpenIddictAuthorizationScopeRecord>>(scopeCmd, ct).ConfigureAwait(false);

        return (authResult.Value?.Count() ?? 0) + (scopeResult.Value?.Count() ?? 0);
    }

    private static async Task HardDeleteAuthorizationRows(Guid authId, DirectDataGateway gateway, CancellationToken ct)
    {
        // Delete child scope rows first.
        var deleteScopesCmd = CmdBuilders.Delete.From("OpenIddictAuthorizationScope")
            .DataStore("AuthDb")
            .Path("auth")
            .Where("AuthorizationId", authId)
            .Build();
        await gateway.Execute<int>(deleteScopesCmd, ct).ConfigureAwait(false);

        // Delete the authorization row.
        var deleteAuthCmd = CmdBuilders.Delete.From("OpenIddictAuthorization")
            .DataStore("AuthDb")
            .Path("auth")
            .Where("Id", authId)
            .Build();
        await gateway.Execute<int>(deleteAuthCmd, ct).ConfigureAwait(false);
    }
}
