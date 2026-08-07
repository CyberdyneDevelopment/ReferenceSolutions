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
/// Real-DB round-trip gate proof for OpenIddictApplicationStore.
/// Uses the REAL FDW MsSql DataGateway against AuthDb on the configured MSSQL_TEST_SERVER (no mocks on the DB path).
/// Skipped automatically when MSSQL_SA_PASSWORD is not set.
/// Proves: Create → GetPermissions reads back → SetPermissions + Update supersedes old set, inserts new.
/// Isolation: try/finally hard-DELETEs ALL rows (all versions) so AuthDb is left exactly as found.
/// </summary>
public sealed class OpenIddictApplicationStoreIntegrationTests
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

    private static (OpenIddictApplicationStore store, MsSqlConnection connection, DirectDataGateway gateway) BuildStore(string password)
    {
        var connectionString = $"Server={ServerAddress},1433;Database={DatabaseName};User Id={LoginName};Password={password};TrustServerCertificate=True;Encrypt=True";
        var msSqlConnection = new MsSqlConnection(
            NullLogger<MsSqlConnection>.Instance,
            new MsSqlConnectionConfiguration(),
            connectionString);

        var applicationContainer = new TestContainer("auth", "OpenIddictApplication", new[]
        {
            ("RowId",           true),
            ("Id",              false),
            ("ClientId",        false),
            ("ClientType",      false),
            ("ConsentType",     false),
            ("DisplayName",     false),
            ("ApplicationType", false),
            ("ClientSecretHash",false),
            ("IsCurrent",       false),
            ("IsDeleted",       false),
            ("CreatedAt",       false),
            ("ModifiedAt",      false)
        });

        var permissionContainer = new TestContainer("auth", "OpenIddictApplicationPermission", new[]
        {
            ("RowId",          true),
            ("ApplicationId",  false),
            ("Permission",     false),
            ("IsCurrent",      false),
            ("IsDeleted",      false),
            ("CreatedAt",      false)
        });

        var redirectUriContainer = new TestContainer("auth", "OpenIddictApplicationRedirectUri", new[]
        {
            ("RowId",         true),
            ("ApplicationId", false),
            ("Uri",           false),
            ("IsCurrent",     false),
            ("IsDeleted",     false),
            ("CreatedAt",     false)
        });

        var postLogoutContainer = new TestContainer("auth", "OpenIddictApplicationPostLogoutRedirectUri", new[]
        {
            ("RowId",         true),
            ("ApplicationId", false),
            ("Uri",           false),
            ("IsCurrent",     false),
            ("IsDeleted",     false),
            ("CreatedAt",     false)
        });

        var requirementContainer = new TestContainer("auth", "OpenIddictApplicationRequirement", new[]
        {
            ("RowId",         true),
            ("ApplicationId", false),
            ("Requirement",   false),
            ("IsCurrent",     false),
            ("IsDeleted",     false),
            ("CreatedAt",     false)
        });

        var containers = new Dictionary<string, IDataContainer>(StringComparer.Ordinal)
        {
            ["OpenIddictApplication"] = applicationContainer,
            ["OpenIddictApplicationPermission"] = permissionContainer,
            ["OpenIddictApplicationRedirectUri"] = redirectUriContainer,
            ["OpenIddictApplicationPostLogoutRedirectUri"] = postLogoutContainer,
            ["OpenIddictApplicationRequirement"] = requirementContainer
        };

        var gateway = new DirectDataGateway(msSqlConnection, containers);
        var store = new OpenIddictApplicationStore(
            new Lazy<IDataGateway>(() => gateway),
            NullLogger<OpenIddictApplicationStore>.Instance);

        return (store, msSqlConnection, gateway);
    }

    /// <summary>
    /// Gate proof: Create application with 2 permissions → GetPermissionsAsync reads them back →
    /// SetPermissionsAsync + UpdateAsync supersedes old set, inserts 1 new permission →
    /// verify via DataGateway query that old rows IsCurrent=0 and new row IsCurrent=1.
    /// After test: SELECT COUNT(*) = 0 for all test rows.
    /// </summary>
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Integration")]
    public async Task CreateApplication_GetPermissions_SetPermissions_VerifiesSupersession()
    {
        var password = GetPassword();
        if (password is null)
        {
            return;
        }

        var ct = TestContext.Current.CancellationToken;
        var (store, connection, gateway) = BuildStore(password);
        using var _ = connection;

        var clientId = $"test.app.{Guid.NewGuid():N}";
        var appId = Guid.Empty;

        try
        {
            // ── Step 1: Create application with 2 permissions ─────────────────────
            var app = await store.InstantiateAsync(ct);
            await store.SetClientIdAsync(app, clientId, ct);
            await store.SetClientTypeAsync(app, "confidential", ct);
            await store.SetDisplayNameAsync(app, "Integration Test App", ct);
            await store.SetPermissionsAsync(app, ImmutableArray.Create("ept:token", "gt:client_credentials"), ct);

            await store.CreateAsync(app, ct);

            app.Id.ShouldNotBe(Guid.Empty);
            appId = app.Id;

            // ── Step 2: GetPermissionsAsync reads back the 2 permissions ──────────
            var permissions = await store.GetPermissionsAsync(app, ct);
            permissions.Length.ShouldBe(2);
            permissions.ShouldContain("ept:token");
            permissions.ShouldContain("gt:client_credentials");

            // ── Step 3: SetPermissionsAsync + UpdateAsync supersedes old set ───────
            await store.SetPermissionsAsync(app, ImmutableArray.Create("ept:token"), ct);
            await store.UpdateAsync(app, ct);

            // ── Step 4: GetPermissionsAsync reflects the new single permission ─────
            var permissionsAfterUpdate = await store.GetPermissionsAsync(app, ct);
            permissionsAfterUpdate.Length.ShouldBe(1);
            permissionsAfterUpdate.ShouldContain("ept:token");

            // ── Step 5: SQL gate proof — verify IsCurrent supersession ────────────
            var allPermRows = await QueryAllPermissionRows(appId, gateway, ct);
            var currentRows = allPermRows.Where(r => r.IsCurrent).ToList();
            var supersededRows = allPermRows.Where(r => !r.IsCurrent).ToList();

            // THE GATE CHECK: 1 current (the retained "ept:token"), 2 superseded (original set)
            currentRows.Count.ShouldBe(1,
                $"IsCurrent=1 should be 1; got {currentRows.Count}. All: [{string.Join(", ", allPermRows.Select(r => $"IsCurrent={r.IsCurrent} Perm={r.Permission}"))}]");
            supersededRows.Count.ShouldBe(2,
                $"IsCurrent=0 should be 2 (original set superseded); got {supersededRows.Count}");

            currentRows[0].Permission.ShouldBe("ept:token");
        }
        finally
        {
            // Hard-DELETE all version rows for this test application so AuthDb is returned to exact pre-test state.
            if (appId != Guid.Empty)
            {
                await HardDeleteApplicationRows(appId, gateway, ct);
            }

            // Verify 0 rows remain.
            var remaining = await CountTestRows(appId, gateway, ct);
            remaining.ShouldBe(0,
                $"AuthDb should have 0 test rows after teardown; found {remaining} orphan(s).");
        }
    }

    private static async Task<IReadOnlyList<OpenIddictApplicationPermissionRecord>> QueryAllPermissionRows(
        Guid appId, DirectDataGateway gateway, CancellationToken ct)
    {
        var command = DataQuery.From<OpenIddictApplicationPermissionRecord>("AuthDb", "auth", "OpenIddictApplicationPermission")
            .Where(r => r.ApplicationId).Equal(appId)
            .Build();

        var result = await gateway.Execute<IEnumerable<OpenIddictApplicationPermissionRecord>>(command, ct)
            .ConfigureAwait(false);
        result.IsSuccess.ShouldBeTrue("verification query should succeed");
        return (result.Value ?? Enumerable.Empty<OpenIddictApplicationPermissionRecord>()).ToList();
    }

    private static async Task<int> CountTestRows(Guid appId, DirectDataGateway gateway, CancellationToken ct)
    {
        var appCmd = DataQuery.From<OpenIddictApplicationRecord>("AuthDb", "auth", "OpenIddictApplication")
            .Where(r => r.Id).Equal(appId)
            .Build();
        var permCmd = DataQuery.From<OpenIddictApplicationPermissionRecord>("AuthDb", "auth", "OpenIddictApplicationPermission")
            .Where(r => r.ApplicationId).Equal(appId)
            .Build();

        var appResult = await gateway.Execute<IEnumerable<OpenIddictApplicationRecord>>(appCmd, ct).ConfigureAwait(false);
        var permResult = await gateway.Execute<IEnumerable<OpenIddictApplicationPermissionRecord>>(permCmd, ct).ConfigureAwait(false);

        return (appResult.Value?.Count() ?? 0) + (permResult.Value?.Count() ?? 0);
    }

    private static async Task HardDeleteApplicationRows(Guid appId, DirectDataGateway gateway, CancellationToken ct)
    {
        // Delete child rows first (permissions, redirect URIs, post-logout URIs, requirements).
        foreach (var childContainer in new[]
        {
            "OpenIddictApplicationPermission",
            "OpenIddictApplicationRedirectUri",
            "OpenIddictApplicationPostLogoutRedirectUri",
            "OpenIddictApplicationRequirement"
        })
        {
            var deleteChildCmd = CmdBuilders.Delete.From(childContainer)
                .DataStore("AuthDb")
                .Path("auth")
                .Where("ApplicationId", appId)
                .Build();
            await gateway.Execute<int>(deleteChildCmd, ct).ConfigureAwait(false);
        }

        // Delete all parent version rows.
        var deleteAppCmd = CmdBuilders.Delete.From("OpenIddictApplication")
            .DataStore("AuthDb")
            .Path("auth")
            .Where("Id", appId)
            .Build();
        await gateway.Execute<int>(deleteAppCmd, ct).ConfigureAwait(false);
    }
}
