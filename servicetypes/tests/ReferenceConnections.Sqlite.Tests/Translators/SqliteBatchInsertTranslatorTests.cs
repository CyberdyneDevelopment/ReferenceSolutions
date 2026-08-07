using Fdw.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Moq;
using Shouldly;
using Xunit;

namespace ReferenceConnections.Sqlite.Tests.Translators;

/// <summary>
/// Verifies multi-row INSERT SQL shape produced by <see cref="SqliteBatchInsertTranslator"/>:
/// double-quote identifiers, schemaless table name, multi-row VALUES syntax, @p0 parameter naming.
/// </summary>
[Collection(nameof(SqliteTestCollection))]
public sealed class SqliteBatchInsertTranslatorTests
{
    private readonly SqliteBatchInsertTranslator _translator = new();

    private sealed class RowData
    {
        public string? name { get; set; }
        public int age { get; set; }
    }

    private static Mock<IDataContainer> CreateContainer(string tableName = "contacts")
    {
        var dbPath = new SqliteDatabasePath(tableName);
        var idField = CreateField("id", isIdentity: true);
        var nameField = CreateField("name");
        var ageField = CreateField("age");

        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields)
            .Returns(new IField[] { idField.Object, nameField.Object, ageField.Object });

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns(tableName);
        container.As<IStorageContainer>().Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.Metadata).Returns(new Dictionary<string, object>(System.StringComparer.Ordinal));
        container.Setup(c => c.Keys).Returns(new List<IContainerKey>());
        container.Setup(c => c.ReferencingKeys)
            .Returns(GenericResult<System.Collections.Generic.IReadOnlyList<ReferencingKeyBinding>>.Success([]));
        return container;
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

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task BatchInsertTranslatorProducesMultiRowValuesSyntax()
    {
        var container = CreateContainer("contacts");
        var rows = new List<RowData>
        {
            new() { name = "Alice", age = 30 },
            new() { name = "Bob", age = 25 }
        };
        var command = new InsertCommand<List<RowData>>(rows);

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("INSERT INTO \"contacts\"");
        // Multi-row VALUES: (@p0, @p1), (@p2, @p3)
        result.Value.CommandText.ShouldContain("VALUES");
        result.Value.CommandText.ShouldContain("),");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task BatchInsertTranslatorUsesAtPrefixParameterNaming()
    {
        var container = CreateContainer("contacts");
        var rows = new List<RowData> { new() { name = "Alice", age = 30 } };
        var command = new InsertCommand<List<RowData>>(rows);

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        // Parameters must use @p0, @p1 ... naming convention.
        result.Value!.CommandText.ShouldContain("@p0");
        result.Value.Parameters.Count.ShouldBeGreaterThan(0);
        result.Value.Parameters[0].ParameterName.ShouldStartWith("@p");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task BatchInsertTranslatorSchemalessTableName()
    {
        var container = CreateContainer("contacts");
        var rows = new List<RowData> { new() { name = "Alice", age = 30 } };
        var command = new InsertCommand<List<RowData>>(rows);

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("INSERT INTO \"contacts\"");
        // No schema qualifier — SQLite is schemaless.
        result.Value.CommandText.ShouldNotContain("\"contacts\".");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataGateway")]
    public async Task BatchInsertTranslatorWithNonEnumerableDataReturnsStructuredFailure()
    {
        // BatchInsert requires an IEnumerable — an int (non-enumerable) must fail-loud.
        var container = CreateContainer("contacts");
        var command = new InsertCommand<int>(42);

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }
}
