using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using ReferenceSecretManagers.Sqlite;
using Fdw.Services.SecretManagers.Sqlite.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Fdw;
using Fdw.Services;
using Fdw.Services.SecretManagers.Sqlite.Commands;

namespace ReferenceSecretManagers.Sqlite.Tests;

/// <summary>
/// Integration tests for <see cref="SqliteSecretManager"/> against real SQLite files.
/// Each test owns its own temp DB so tests can run in parallel without interference.
/// IAsyncLifetime manages the shared DB used by the NotFound test.
/// </summary>
public sealed class SqliteSecretManagerIntegrationTests : IAsyncLifetime
{
    private string _sharedDbPath = string.Empty;
    private ISecretManager _sharedManager = null!;

    // ── Fixture lifecycle ─────────────────────────────────────────────────────

    public async ValueTask InitializeAsync()
    {
        _sharedDbPath = TempDb(nameof(SqliteSecretManagerIntegrationTests) + "-shared");
        _sharedManager = await CreateManagerForPath(_sharedDbPath);
    }

    public ValueTask DisposeAsync()
    {
        ((IDisposable)_sharedManager).Dispose();
        CleanupDb(_sharedDbPath);
        return ValueTask.CompletedTask;
    }

    // ── Test 1: Round-trip Set → Get ──────────────────────────────────────────

    [Fact]
    public async Task SetAndGet_Roundtrip_ValueAndKeyMatch()
    {
        var dbPath = TempDb(nameof(SetAndGet_Roundtrip_ValueAndKeyMatch));
        var ct = TestContext.Current.CancellationToken;
        var manager = await CreateManagerForPath(dbPath);
        try
        {
            var setResult = await manager.Execute<SecretValue>(MakeSetCommand("rt:key", "rt:super-secret"), ct);
            setResult.IsSuccess.ShouldBeTrue(FormatMessages(setResult));

            var getResult = await manager.Execute<SecretValue>(GetSecretManagerCommand.Latest(null, "rt:key"), ct);
            getResult.IsSuccess.ShouldBeTrue(FormatMessages(getResult));
            getResult.Value.ShouldNotBeNull();
            getResult.Value!.Key.ShouldBe("rt:key");
            getResult.Value!.GetStringValue().ShouldBe("rt:super-secret");
        }
        finally
        {
            ((IDisposable)manager).Dispose();
            CleanupDb(dbPath);
        }
    }

    // ── Test 2: Version-on-write ──────────────────────────────────────────────

    [Fact]
    public async Task SetTwice_VersionOnWrite_OldRowDeactivatedNewRowCurrent()
    {
        var dbPath = TempDb(nameof(SetTwice_VersionOnWrite_OldRowDeactivatedNewRowCurrent));
        var ct = TestContext.Current.CancellationToken;
        var manager = await CreateManagerForPath(dbPath);
        try
        {
            var set1 = await manager.Execute<SecretValue>(MakeSetCommand("vow:key", "vow:v1"), ct);
            set1.IsSuccess.ShouldBeTrue(FormatMessages(set1));
            var set2 = await manager.Execute<SecretValue>(MakeSetCommand("vow:key", "vow:v2"), ct);
            set2.IsSuccess.ShouldBeTrue(FormatMessages(set2));

            var rows = await QueryAllRowsForKeyAsync(dbPath, "vow:key", ct);
            rows.Count.ShouldBe(2, "version-on-write must produce two rows");

            var oldRow = rows.SingleOrDefault(r => r.Version == 1);
            oldRow.ShouldNotBeNull("version-1 row must exist");
            oldRow!.IsCurrent.ShouldBeFalse("old row must have IsCurrent=0");
            oldRow!.IsDeleted.ShouldBeFalse("old row must not be soft-deleted");

            var newRow = rows.SingleOrDefault(r => r.Version == 2);
            newRow.ShouldNotBeNull("version-2 row must exist");
            newRow!.IsCurrent.ShouldBeTrue("new row must have IsCurrent=1");
            newRow!.IsDeleted.ShouldBeFalse("new row must not be soft-deleted");
            newRow!.SecretValue.ShouldBe("vow:v2");
        }
        finally
        {
            ((IDisposable)manager).Dispose();
            CleanupDb(dbPath);
        }
    }

    // ── Test 3: Soft delete ───────────────────────────────────────────────────

    [Fact]
    public async Task Delete_SoftDelete_GetReturnsNotFoundAndRowFlagged()
    {
        var dbPath = TempDb(nameof(Delete_SoftDelete_GetReturnsNotFoundAndRowFlagged));
        var ct = TestContext.Current.CancellationToken;
        var manager = await CreateManagerForPath(dbPath);
        try
        {
            var setResult = await manager.Execute<SecretValue>(MakeSetCommand("del:key", "del:value"), ct);
            setResult.IsSuccess.ShouldBeTrue(FormatMessages(setResult));

            var deleteResult = await manager.Execute<IGenericResult>(new DeleteSecretManagerCommand(null, "del:key"), ct);
            deleteResult.IsSuccess.ShouldBeTrue(FormatMessages(deleteResult));

            var getResult = await manager.Execute<SecretValue>(GetSecretManagerCommand.Latest(null, "del:key"), ct);
            getResult.IsSuccess.ShouldBeFalse("deleted secret must not be retrievable");

            var rows = await QueryAllRowsForKeyAsync(dbPath, "del:key", ct);
            rows.Count.ShouldBe(1, "soft-delete must leave the row in place with flags set");
            rows[0].IsCurrent.ShouldBeFalse("soft-deleted row must have IsCurrent=0");
            rows[0].IsDeleted.ShouldBeTrue("soft-deleted row must have IsDeleted=1");
        }
        finally
        {
            ((IDisposable)manager).Dispose();
            CleanupDb(dbPath);
        }
    }

    // ── Test 4: ListSecrets returns only current / non-deleted ────────────────

    [Fact]
    public async Task ListSecrets_OnlyCurrentNonDeletedKeysReturned()
    {
        var dbPath = TempDb(nameof(ListSecrets_OnlyCurrentNonDeletedKeysReturned));
        var ct = TestContext.Current.CancellationToken;
        var manager = await CreateManagerForPath(dbPath);
        try
        {
            var s1 = await manager.Execute<SecretValue>(MakeSetCommand("lst:key1", "lst:value1"), ct);
            s1.IsSuccess.ShouldBeTrue(FormatMessages(s1));
            var s2 = await manager.Execute<SecretValue>(MakeSetCommand("lst:key2", "lst:value2"), ct);
            s2.IsSuccess.ShouldBeTrue(FormatMessages(s2));
            var s3 = await manager.Execute<SecretValue>(MakeSetCommand("lst:key3", "lst:value3"), ct);
            s3.IsSuccess.ShouldBeTrue(FormatMessages(s3));

            var del = await manager.Execute<IGenericResult>(new DeleteSecretManagerCommand(null, "lst:key3"), ct);
            del.IsSuccess.ShouldBeTrue(FormatMessages(del));

            var listResult = await manager.Execute<IReadOnlyList<ISecretMetadata>>(ListSecretsManagerCommand.All(null), ct);
            listResult.IsSuccess.ShouldBeTrue(FormatMessages(listResult));

            var keys = listResult.Value!.Select(m => m.Key).ToList();
            keys.ShouldContain("lst:key1");
            keys.ShouldContain("lst:key2");
            keys.ShouldNotContain("lst:key3", "deleted secret must not appear in list");
        }
        finally
        {
            ((IDisposable)manager).Dispose();
            CleanupDb(dbPath);
        }
    }

    // ── Test 5: Table auto-create on fresh DB ─────────────────────────────────

    [Fact]
    public async Task FreshDb_TableAutoCreatedAndFirstOperationSucceeds()
    {
        var dbPath = TempDb(nameof(FreshDb_TableAutoCreatedAndFirstOperationSucceeds));
        var ct = TestContext.Current.CancellationToken;
        var manager = await CreateManagerForPath(dbPath);
        try
        {
            var setResult = await manager.Execute<SecretValue>(MakeSetCommand("auto:key", "auto:value"), ct);
            setResult.IsSuccess.ShouldBeTrue(FormatMessages(setResult));

            var getResult = await manager.Execute<SecretValue>(GetSecretManagerCommand.Latest(null, "auto:key"), ct);
            getResult.IsSuccess.ShouldBeTrue(FormatMessages(getResult));
            getResult.Value!.GetStringValue().ShouldBe("auto:value");
        }
        finally
        {
            ((IDisposable)manager).Dispose();
            CleanupDb(dbPath);
        }
    }

    // ── Test 6: NotFound returns structured failure, does not throw ───────────

    [Fact]
    public async Task GetNonExistentKey_ReturnsFailureResult_NoThrow()
    {
        var ct = TestContext.Current.CancellationToken;

        var getResult = await _sharedManager.Execute<SecretValue>(
            GetSecretManagerCommand.Latest(null, "notfound:key-xyz-no-such-secret"), ct);

        getResult.IsSuccess.ShouldBeFalse("non-existent key must return failure");
        getResult.Messages.ShouldNotBeEmpty("failure result must carry at least one message");
    }

    // ── Test 7: Security — no secret value in any log message ────────────────

    [Fact]
    public async Task SetGetDelete_NoSecretValueAppearsInLogs()
    {
        const string canaryValue = "SUPER_SECRET_CANARY_abc123_MUST_NOT_LOG";
        var capturingFactory = new CapturingLoggerFactory();
        var dbPath = TempDb(nameof(SetGetDelete_NoSecretValueAppearsInLogs));
        var ct = TestContext.Current.CancellationToken;
        var manager = await CreateManagerForPath(dbPath, capturingFactory);
        try
        {
            var setResult = await manager.Execute<SecretValue>(MakeSetCommand("sec:key", canaryValue), ct);
            setResult.IsSuccess.ShouldBeTrue(FormatMessages(setResult));

            await manager.Execute<SecretValue>(GetSecretManagerCommand.Latest(null, "sec:key"), ct);
            await manager.Execute<IGenericResult>(new DeleteSecretManagerCommand(null, "sec:key"), ct);
        }
        finally
        {
            ((IDisposable)manager).Dispose();
            CleanupDb(dbPath);
        }

        capturingFactory.Messages.ShouldNotBeEmpty("logger must have captured at least one message");
        foreach (var message in capturingFactory.Messages)
        {
            message.ShouldNotContain(canaryValue, Case.Sensitive,
                $"Secret value must never appear in logs. Found in: {message}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string TempDb(string testName)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sqlitesecrets-{testName}.db");
        if (File.Exists(path)) File.Delete(path);
        return path;
    }

    private static void CleanupDb(string dbPath)
    {
        if (File.Exists(dbPath)) File.Delete(dbPath);
    }

    private static SetSecretManagerCommand MakeSetCommand(string key, string value)
        => new(
            container: null,
            secretKey: key,
            secretValue: value,
            parameters: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["SecretType"] = "Password"
            });

    private static async Task<ISecretManager> CreateManagerForPath(
        string dbPath,
        ILoggerFactory? loggerFactory = null)
    {
        var config = new SqliteSecretManagerConfiguration
        {
            Id = Guid.NewGuid(),
            SecretManagerId = Guid.NewGuid(),
            DataSource = dbPath,
            TableName = "Secret",
            CommandTimeoutSeconds = 30
        };
        var factory = new SqliteSecretManagerFactory(loggerFactory);
        var result = await factory.CreateSecretManager(config).ConfigureAwait(false);
        result.IsSuccess.ShouldBeTrue($"Manager creation failed: {FormatMessages(result)}");
        return result.Value!;
    }

    private static async Task<List<DbSecretRow>> QueryAllRowsForKeyAsync(
        string dbPath, string key, CancellationToken ct)
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
        var conn = new SqliteConnection(connectionString);
        await using (conn.ConfigureAwait(false))
        {
            await conn.OpenAsync(ct).ConfigureAwait(false);
            var cmd = conn.CreateCommand();
            await using (cmd.ConfigureAwait(false))
            {
                cmd.CommandText =
                    "SELECT \"SecretValue\", \"Version\", \"IsCurrent\", \"IsDeleted\" " +
                    "FROM \"Secret\" " +
                    "WHERE \"SecretKey\" = @key " +
                    "ORDER BY \"Version\"";
                cmd.Parameters.AddWithValue("@key", key);
                var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false))
                {
                    var rows = new List<DbSecretRow>();
                    while (await reader.ReadAsync(ct).ConfigureAwait(false))
                    {
                        rows.Add(new DbSecretRow(
                            SecretValue: reader.GetString(0),
                            Version: reader.GetInt32(1),
                            IsCurrent: reader.GetInt32(2) == 1,
                            IsDeleted: reader.GetInt32(3) == 1));
                    }
                    return rows;
                }
            }
        }
    }

    private static string FormatMessages<T>(IGenericResult<T> result)
        => string.Join("; ", result.Messages.Select(m => m.Message));

    private static string FormatMessages(IGenericResult result)
        => string.Join("; ", result.Messages.Select(m => m.Message));

    private sealed record DbSecretRow(
        string SecretValue,
        int Version,
        bool IsCurrent,
        bool IsDeleted);
}
