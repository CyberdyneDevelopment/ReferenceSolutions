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
/// Verifies INSERT SQL shape produced by <see cref="SqliteInsertTranslator"/>:
/// double-quote identifiers, schemaless table name, @-prefixed params, identity exclusion.
/// </summary>
[Collection(nameof(SqliteTestCollection))]
public sealed class SqliteInsertTranslatorTests
{
    private readonly SqliteInsertTranslator _translator = new();

    private sealed class ContactData
    {
        public int id { get; set; }
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
    public async Task InsertTranslatorProducesDoubleQuotedColumnNames()
    {
        var container = CreateContainer("contacts");
        var command = new InsertCommand<ContactData>(new ContactData { name = "Alice", age = 30 });

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("\"name\"");
        result.Value.CommandText.ShouldContain("\"age\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task InsertTranslatorSchemalessTableName()
    {
        var container = CreateContainer("contacts");
        var command = new InsertCommand<ContactData>(new ContactData { name = "Alice", age = 30 });

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        // SQLite is schemaless — table name must be bare "contacts", no dot-qualified prefix.
        result.Value!.CommandText.ShouldContain("INSERT INTO \"contacts\"");
        result.Value.CommandText.ShouldNotContain(".");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task InsertTranslatorExcludesIdentityFieldFromColumnList()
    {
        var container = CreateContainer("contacts");
        var command = new InsertCommand<ContactData>(new ContactData { id = 99, name = "Alice", age = 30 });

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        // "id" is isIdentity=true — must NOT appear in the INSERT column list.
        result.Value!.CommandText.ShouldNotContain("\"id\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task InsertTranslatorParametersUseAtPrefix()
    {
        var container = CreateContainer("contacts");
        var command = new InsertCommand<ContactData>(new ContactData { name = "Alice", age = 30 });

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("@name");
        result.Value.CommandText.ShouldContain("@age");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataGateway")]
    public async Task InsertTranslatorWithNullDataReturnsStructuredFailure()
    {
        var container = CreateContainer("contacts");
        var command = new InsertCommand<ContactData?>(null);

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataGateway")]
    public async Task InsertTranslatorWithInvalidContainerPathReturnsStructuredFailure()
    {
        var badPath = new Mock<IPath>();
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(System.Array.Empty<IField>());

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("contacts");
        container.As<IStorageContainer>().Setup(c => c.Path).Returns(badPath.Object);
        container.Setup(c => c.Schema).Returns(schema.Object);

        var command = new InsertCommand<ContactData>(new ContactData { name = "Alice", age = 30 });

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }
}
