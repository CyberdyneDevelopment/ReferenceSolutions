using Fdw.Data.Sqlite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sqlite;
using FdwReferenceConnections.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.Script.Tests.Integration;

/// <summary>
/// Scripts run against a real SQLite file (FDW-1441): a command changes rows and answers how many, a query answers its rows
/// by column name, and every value travels as a parameter.
/// </summary>
[Collection(nameof(ScriptTestCollection))]
public sealed class SqliteScriptedTests : IAsyncLifetime
{
    private static readonly string DbPath = Path.Combine(Path.GetTempPath(), "fdw-sqlite-scripted-tests.db");

    private IScriptedDataConnection? _connection;

    public async ValueTask InitializeAsync()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(DbPath)) File.Delete(DbPath);

        using (var adoConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={DbPath}"))
        {
            await adoConn.OpenAsync().ConfigureAwait(false);
            using var cmd = adoConn.CreateCommand();
            cmd.CommandText = "CREATE TABLE teams (id INTEGER PRIMARY KEY, name TEXT NOT NULL, founded INTEGER NULL)";
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        var factory = new SqliteConnectionFactory(NullLogger<SqliteConnectionFactory>.Instance, NullLogger<FdwReferenceConnections.Sqlite.SqliteConnection>.Instance);
        var created = factory.Create(new SqliteConnectionConfiguration { DataSource = DbPath, Mode = "ReadWriteCreate" }, resolvedSecret: null);
        created.IsSuccess.ShouldBeTrue(created.CurrentMessage);
        _connection = new SqliteScriptConnection(created.Value.ShouldBeOfType<FdwReferenceConnections.Sqlite.SqliteConnection>());
    }

    public ValueTask DisposeAsync()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(DbPath)) File.Delete(DbPath);
        return ValueTask.CompletedTask;
    }

    private async Task<int> Insert(int id, string name, int? founded)
    {
        var result = await _connection!.Execute(
            new ScriptRunCommand(
                "INSERT INTO teams (id, name, founded) VALUES (@Id, @Name, @Founded)",
                [new ScriptParameter("Id", id), new ScriptParameter("Name", name), new ScriptParameter("Founded", founded)]),
            TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        return result.Value;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task ARunScriptAnswersTheRowsItAffected()
    {
        (await Insert(1, "Bills", 1960)).ShouldBe(1);
        (await Insert(2, "Jets", 1960)).ShouldBe(1);

        var result = await _connection!.Execute(
            new ScriptRunCommand("UPDATE teams SET founded = @Year WHERE founded = @Old", [new ScriptParameter("Year", 1961), new ScriptParameter("Old", 1960)]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task AReadScriptAnswersItsRowsByColumnNameAndADatabaseNullIsNull()
    {
        await Insert(1, "Bills", 1960);
        await Insert(2, "Giants", null);

        var result = await _connection!.Execute(
            new ScriptReadCommand("SELECT id, name AS Team, founded FROM teams WHERE id >= @From ORDER BY id", [new ScriptParameter("From", 1)], null),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.Select(row => row["Team"]).ShouldBe(["Bills", "Giants"]);
        result.Value[0]["founded"].ShouldBe(1960L);
        result.Value[1]["founded"].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AValueThatLooksLikeSqlIsStoredAsTextAndRunsNothing()
    {
        const string hostile = "x'); DROP TABLE teams; --";
        await Insert(1, hostile, 1960);

        var result = await _connection!.Execute(
            new ScriptReadCommand("SELECT name FROM teams", [], null), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.Single()["name"].ShouldBe(hostile);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AReadThatSelectsMoreThanItsTakeFails()
    {
        await Insert(1, "Bills", 1960);
        await Insert(2, "Jets", 1960);

        var result = await _connection!.Execute(
            new ScriptReadCommand("SELECT name FROM teams", [], 1), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AScriptTheDatabaseRefusesFailsWithItsReason()
    {
        var result = await _connection!.Execute(
            new ScriptReadCommand("SELECT * FROM no_such_table", [], null), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
    }
}
