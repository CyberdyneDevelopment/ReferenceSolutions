using Fdw.Data.Sqlite;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace ReferenceConnections.Sqlite.Tests.Integration;

/// <summary>
/// Full Execute&lt;T&gt; round-trip integration tests against a real SQLite file database.
/// Uses the factory → IDataConnection.Execute&lt;T&gt; path; no raw ADO.NET in the data operations.
/// </summary>
[Collection(nameof(SqliteTestCollection))]
public sealed class SqliteConnectionRoundTripTests : IAsyncLifetime
{
    // Fixed unique path: one file per test run, recreated fresh in InitializeAsync.
    private static readonly string DbPath = Path.Combine(Path.GetTempPath(), "fdw-sqlite-roundtrip-tests.db");

    private readonly SqliteConnectionFactory _factory;
    private readonly SqliteConnectionConfiguration _config;
    private readonly IDataContainer _container;
    private IDataConnection? _connection;

    public SqliteConnectionRoundTripTests()
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
        _connection = (IDataConnection)factoryResult.Value!;
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
    public void FactoryCreatesConnectionSuccessfully()
    {
        var result = _factory.Create(_config);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task InsertQueryUpdateDeleteRoundTrip()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = _connection!;

        // INSERT one row.
        var insertResult = await connection.Execute<int>(
            new InsertCommand<ContactRow>(new ContactRow { name = "Alice", age = 30 }),
            _container,
            ct);
        insertResult.IsSuccess.ShouldBeTrue();
        insertResult.Value.ShouldBe(1);

        // QUERY back — expect one row with the inserted values.
        var queryResult = await connection.Execute<IEnumerable<Dictionary<string, object?>>>(
            new QueryCommand<object>(),
            _container,
            ct);
        queryResult.IsSuccess.ShouldBeTrue();
        var rows = queryResult.Value!.ToList();
        rows.Count.ShouldBe(1);
        rows[0]["name"].ShouldBe("Alice");
        // SQLite returns INTEGER columns as long.
        rows[0]["age"].ShouldBe(30L);

        // Capture generated id for UPDATE.
        var id = (long)rows[0]["id"]!;

        // UPDATE — modify name and age by primary key (no filter; translator uses SurrogateKeyField).
        var updateResult = await connection.Execute<int>(
            new UpdateCommand<ContactRowWithId>(new ContactRowWithId { id = id, name = "Alice Updated", age = 31 }),
            _container,
            ct);
        updateResult.IsSuccess.ShouldBeTrue();
        updateResult.Value.ShouldBe(1);

        // QUERY again — verify update.
        var queryAfterUpdate = await connection.Execute<IEnumerable<Dictionary<string, object?>>>(
            new QueryCommand<object>(),
            _container,
            ct);
        queryAfterUpdate.IsSuccess.ShouldBeTrue();
        var updatedRows = queryAfterUpdate.Value!.ToList();
        updatedRows.Count.ShouldBe(1);
        updatedRows[0]["name"].ShouldBe("Alice Updated");
        updatedRows[0]["age"].ShouldBe(31L);

        // DELETE by id filter.
        var deleteFilter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "id",
                Operator = FilterOperators.ByName("Equal"),
                Value = id
            }
        };
        var deleteResult = await connection.Execute<int>(
            new DeleteCommand { Filter = deleteFilter },
            _container,
            ct);
        deleteResult.IsSuccess.ShouldBeTrue();
        deleteResult.Value.ShouldBe(1);

        // QUERY final — table must be empty.
        var queryAfterDelete = await connection.Execute<IEnumerable<Dictionary<string, object?>>>(
            new QueryCommand<object>(),
            _container,
            ct);
        queryAfterDelete.IsSuccess.ShouldBeTrue();
        queryAfterDelete.Value!.ToList().Count.ShouldBe(0);
    }

    private static IDataContainer BuildContainer()
    {
        var dbPath = new SqliteDatabasePath("contacts");
        var idField = CreateField("id", isIdentity: true);
        var nameField = CreateField("name");
        var ageField = CreateField("age");

        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(new IField[] { idField.Object, nameField.Object, ageField.Object });

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

    private sealed class ContactRowWithId
    {
        public long id { get; set; }
        public string? name { get; set; }
        public int age { get; set; }
    }
}
