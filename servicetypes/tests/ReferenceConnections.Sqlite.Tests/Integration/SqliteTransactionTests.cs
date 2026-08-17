using Fdw.Data.Sqlite;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace ReferenceConnections.Sqlite.Tests.Integration;

/// <summary>
/// Integration tests for SQLite transactional behavior: Commit persists, Rollback discards,
/// and Dispose-without-Commit discards. Uses a real SQLite file database.
/// </summary>
[Collection(nameof(SqliteTestCollection))]
public sealed class SqliteTransactionTests : IAsyncLifetime
{
    private static readonly string DbPath = Path.Combine(Path.GetTempPath(), "fdw-sqlite-transaction-tests.db");

    private readonly SqliteConnectionFactory _factory;
    private readonly SqliteConnectionConfiguration _config;
    private readonly IDataContainer _container;
    private ITransactionalDataConnection? _connection;

    public SqliteTransactionTests()
    {
        _factory = new SqliteConnectionFactory(
            NullLogger<SqliteConnectionFactory>.Instance,
            NullLogger<SqliteConnection>.Instance);

        _config = new SqliteConnectionConfiguration
        {
            DataSource = DbPath,
            Mode = "ReadWriteCreate"
        };

        _container = BuildContainer();
    }

    public async ValueTask InitializeAsync()
    {
        // Clear connection pool so stale handles from a previous test don't cause "readonly database"
        // errors when we delete and recreate the file.
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (File.Exists(DbPath)) File.Delete(DbPath);

        // Create table using direct ADO (DDL only — not an FDW data operation).
        using var adoConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={DbPath}");
        await adoConn.OpenAsync().ConfigureAwait(false);
        using var cmd = adoConn.CreateCommand();
        cmd.CommandText =
            "CREATE TABLE contacts (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, age INTEGER NOT NULL)";
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

        var factoryResult = _factory.Create(_config);
        factoryResult.IsSuccess.ShouldBeTrue();
        _connection = (ITransactionalDataConnection)factoryResult.Value!;
    }

    public ValueTask DisposeAsync()
    {
        if (File.Exists(DbPath))
            File.Delete(DbPath);
        return ValueTask.CompletedTask;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task QueryInsideTransactionReturnsDictionaryRows()
    {
        // Proves the Dictionary<string,object?> path works via transaction.Execute<T> —
        // the gap that existed before MapReaderToTypeInternal<T> was shared.
        var ct = TestContext.Current.CancellationToken;

        var transactionResult = await _connection!.BeginTransaction(ct);
        transactionResult.IsSuccess.ShouldBeTrue();

        await using var transaction = transactionResult.Value!;

        var insertResult = await transaction.Execute(
            new InsertCommand<ContactRow>(new ContactRow { name = "Eve", age = 28 }),
            _container,
            ct);
        insertResult.IsSuccess.ShouldBeTrue();

        // Query INSIDE the same transaction — must see the uncommitted row.
        var queryResult = await transaction.Execute<IEnumerable<Dictionary<string, object?>>>(
            new QueryCommand<object>(),
            _container,
            ct);
        queryResult.IsSuccess.ShouldBeTrue();
        var rows = queryResult.Value!.ToList();
        rows.Count.ShouldBe(1);
        rows[0]["name"].ShouldBe("Eve");

        // Rollback so the row doesn't persist.
        await transaction.Rollback(ct);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task CommitPersistsInsertedRow()
    {
        var ct = TestContext.Current.CancellationToken;

        var transactionResult = await _connection!.BeginTransaction(ct);
        transactionResult.IsSuccess.ShouldBeTrue();

        await using var transaction = transactionResult.Value!;

        var insertResult = await transaction.Execute(
            new InsertCommand<ContactRow>(new ContactRow { name = "Bob", age = 25 }),
            _container,
            ct);
        insertResult.IsSuccess.ShouldBeTrue();

        var commitResult = await transaction.Commit(ct);
        commitResult.IsSuccess.ShouldBeTrue();

        // Query via a fresh connection — the committed row must be visible.
        var rows = await QueryAllRows(ct);
        rows.Count.ShouldBe(1);
        rows[0]["name"].ShouldBe("Bob");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task RollbackDiscardsInsertedRow()
    {
        var ct = TestContext.Current.CancellationToken;

        var transactionResult = await _connection!.BeginTransaction(ct);
        transactionResult.IsSuccess.ShouldBeTrue();

        await using var transaction = transactionResult.Value!;

        var insertResult = await transaction.Execute(
            new InsertCommand<ContactRow>(new ContactRow { name = "Carol", age = 35 }),
            _container,
            ct);
        insertResult.IsSuccess.ShouldBeTrue();

        var rollbackResult = await transaction.Rollback(ct);
        rollbackResult.IsSuccess.ShouldBeTrue();

        // Query via a fresh connection — the rolled-back row must NOT be visible.
        var rows = await QueryAllRows(ct);
        rows.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task DisposeWithoutCommitDiscardsInsertedRow()
    {
        var ct = TestContext.Current.CancellationToken;

        var transactionResult = await _connection!.BeginTransaction(ct);
        transactionResult.IsSuccess.ShouldBeTrue();

        // Insert within the transaction, then let DisposeAsync handle the implicit rollback.
        await using (var transaction = transactionResult.Value!)
        {
            var insertResult = await transaction.Execute(
                new InsertCommand<ContactRow>(new ContactRow { name = "Dave", age = 40 }),
                _container,
                ct);
            insertResult.IsSuccess.ShouldBeTrue();
            // Intentionally no Commit() — dispose should roll back implicitly.
        }

        // Query via a fresh connection — the row must NOT be visible.
        var rows = await QueryAllRows(ct);
        rows.Count.ShouldBe(0);
    }

    private async Task<List<Dictionary<string, object?>>> QueryAllRows(System.Threading.CancellationToken ct)
    {
        // Use a fresh factory-created connection (not the same ADO connection as the transaction).
        var connResult = _factory.Create(_config);
        connResult.IsSuccess.ShouldBeTrue();

        var freshConnection = (IDataConnection)connResult.Value!;
        var queryResult = await freshConnection.Execute<IEnumerable<Dictionary<string, object?>>>(
            new QueryCommand<object>(),
            _container,
            ct);
        queryResult.IsSuccess.ShouldBeTrue();
        return queryResult.Value!.ToList();
    }

    private static IDataContainer BuildContainer()
    {
        var dbPath = new SqliteDatabasePath("contacts");
        var idField = CreateField("id", isIdentity: true);
        var nameField = CreateField("name");
        var ageField = CreateField("age");

        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(new IField[] { idField.Object, nameField.Object, ageField.Object });
        schema.Setup(s => s.GetProjectableFields()).Returns(new IField[] { idField.Object, nameField.Object, ageField.Object });

        var metadata = new Dictionary<string, object>(System.StringComparer.Ordinal)
        {
            ["SurrogateKeyField"] = "id"
        };

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("contacts");
        container.As<IStorageContainer>().Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.Metadata).Returns(metadata);
        container.Setup(c => c.Keys).Returns(new List<IContainerKey>());
        container.Setup(c => c.ReferencingKeys)
            .Returns(GenericResult<System.Collections.Generic.IReadOnlyList<ReferencingKeyBinding>>.Success([]));
        return container.Object;
    }

    private static Mock<IField> CreateField(string name, bool isIdentity = false)
    {
        var f = new Mock<IField>();
        f.Setup(x => x.Name).Returns(name);
        f.Setup(x => x.Visibility).Returns(FieldVisibilities.ByName("Visible"));
        f.Setup(x => x.IsIdentity).Returns(isIdentity);
        f.Setup(x => x.IsComputed).Returns(false);
        f.Setup(x => x.IsSystemProvided).Returns(false);
        return f;
    }

    private sealed class ContactRow
    {
        public string? name { get; set; }
        public int age { get; set; }
    }
}
