using Fdw.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Moq;
using Shouldly;
using Xunit;

namespace ReferenceConnections.Sqlite.Tests.Translators;

/// <summary>
/// Verifies UPDATE SQL shape produced by <see cref="SqliteUpdateTranslator"/>:
/// double-quote identifiers, schemaless table name, SET clause excludes identity fields.
/// </summary>
[Collection(nameof(SqliteTestCollection))]
public sealed class SqliteUpdateTranslatorTests
{
    private readonly SqliteUpdateTranslator _translator = new();

    private sealed class ContactData
    {
        public long id { get; set; }
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

        var metadata = new Dictionary<string, object>(System.StringComparer.Ordinal)
        {
            ["SurrogateKeyField"] = "id"
        };

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns(tableName);
        container.As<IStorageContainer>().Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.Metadata).Returns(metadata);
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
    public async Task UpdateTranslatorProducesDoubleQuotedSetClause()
    {
        var container = CreateContainer("contacts");
        var command = new UpdateCommand<ContactData>(new ContactData { id = 1, name = "Bob", age = 31 });

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("UPDATE \"contacts\" SET");
        result.Value.CommandText.ShouldContain("\"name\"");
        result.Value.CommandText.ShouldContain("\"age\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task UpdateTranslatorExcludesIdentityFieldFromSetClause()
    {
        var container = CreateContainer("contacts");
        var command = new UpdateCommand<ContactData>(new ContactData { id = 1, name = "Bob", age = 31 });

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        // "id" is marked isIdentity; it must never appear in the SET clause.
        var setClause = result.Value!.CommandText;
        // Only check the SET portion — the WHERE clause may reference "id".
        var setIndex = setClause.IndexOf("SET", System.StringComparison.Ordinal);
        var whereIndex = setClause.IndexOf("WHERE", System.StringComparison.Ordinal);
        var setBody = whereIndex > setIndex
            ? setClause.Substring(setIndex, whereIndex - setIndex)
            : setClause.Substring(setIndex);
        setBody.ShouldNotContain("\"id\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task UpdateTranslatorSchemalessTableName()
    {
        var container = CreateContainer("contacts");
        var command = new UpdateCommand<ContactData>(new ContactData { id = 1, name = "Bob", age = 31 });

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldStartWith("UPDATE \"contacts\"");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataGateway")]
    public async Task UpdateTranslatorWithFilterProducesWhereClause()
    {
        var container = CreateContainer("contacts");
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "name",
                Operator = FilterOperators.ByName("Equal"),
                Value = "Alice"
            }
        };
        var command = new UpdateCommand<ContactData>(new ContactData { name = "Bob", age = 31 }) { Filter = filter };

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE");
        result.Value.CommandText.ShouldContain("\"name\"");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataGateway")]
    public async Task UpdateTranslatorWithInvalidContainerPathReturnsStructuredFailure()
    {
        var badPath = new Mock<IPath>();
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(System.Array.Empty<IField>());

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("contacts");
        container.As<IStorageContainer>().Setup(c => c.Path).Returns(badPath.Object);
        container.Setup(c => c.Schema).Returns(schema.Object);

        var command = new UpdateCommand<ContactData>(new ContactData { name = "Bob", age = 31 });

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }
}
