using Fdw.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Data.Sqlite;

namespace FdwReferenceConnections.Sqlite.Tests.Translators;

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

    private sealed class NameOnlyData
    {
        public string? name { get; set; }
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
        container.As<IStorageContainer>().Setup(c => c.Name).Returns(tableName);
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

    private static object ParameterValue(SqliteCommand command, string name)
        => command.Parameters.Cast<SqliteParameter>().Single(p => p.ParameterName == "@" + name).Value!;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task UpdateTranslatorSetsTheColumnsADictionaryRecordNames()
    {
        // FDW-3309: a pipeline Update load sends each row as IDictionary<string, object?>. The translator
        // looked for CLR properties and found none, so the row failed with "no properties matching".
        var container = CreateContainer("contacts");
        IDictionary<string, object?> record = new Dictionary<string, object?> { ["id"] = 7L, ["name"] = "Bob", ["age"] = 31 };
        var command = new UpdateCommand<IDictionary<string, object?>>(record);

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("\"name\" = @set_name");
        result.Value.CommandText.ShouldContain("\"age\" = @set_age");
        result.Value.CommandText.ShouldContain("WHERE \"id\" = @where_pk");
        ParameterValue(result.Value, "set_name").ShouldBe("Bob");
        ParameterValue(result.Value, "where_pk").ShouldBe(7L);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task UpdateTranslatorSetsOnlyTheColumnsADictionaryRecordCarries()
    {
        var container = CreateContainer("contacts");
        IDictionary<string, object?> record = new Dictionary<string, object?> { ["id"] = 7L, ["age"] = 40, ["not_a_column"] = 1 };
        var command = new UpdateCommand<IDictionary<string, object?>>(record);

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("\"age\" = @set_age");
        result.Value.CommandText.ShouldNotContain("@set_name");
        result.Value.CommandText.ShouldNotContain("not_a_column");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task UpdateTranslatorBindsANullInADictionaryRecordAsDbNull()
    {
        var container = CreateContainer("contacts");
        IDictionary<string, object?> record = new Dictionary<string, object?> { ["id"] = 7L, ["name"] = null };
        var command = new UpdateCommand<IDictionary<string, object?>>(record);

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        ParameterValue(result.Value!, "set_name").ShouldBe(System.DBNull.Value);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task UpdateTranslatorFailsWhenADictionaryRecordNamesNoUpdatableColumn()
    {
        var container = CreateContainer("contacts");
        IDictionary<string, object?> record = new Dictionary<string, object?> { ["id"] = 7L, ["other"] = "x" };
        var command = new UpdateCommand<IDictionary<string, object?>>(record);

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task UpdateTranslatorRefusesAnEntityRecordWithoutThePrimaryKeyWhenThereIsNoFilter()
    {
        // FDW-3321: with no filter and no key in the record there is no WHERE to build. The statement used
        // to be issued without one and wrote every row of the table.
        var container = CreateContainer("contacts");
        var command = new UpdateCommand<NameOnlyData>(new NameOnlyData { name = "Bob" });

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("MissingPrimaryKeyValue");
        result.Details!.GetValue<string>("Container").ShouldBe("contacts");
        result.Details.GetValue<string>("PrimaryKeyField").ShouldBe("id");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task UpdateTranslatorRefusesADictionaryRecordWithoutThePrimaryKeyWhenThereIsNoFilter()
    {
        var container = CreateContainer("contacts");
        IDictionary<string, object?> record = new Dictionary<string, object?> { ["name"] = "Bob" };
        var command = new UpdateCommand<IDictionary<string, object?>>(record);

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("MissingPrimaryKeyValue");
        result.Details!.GetValue<string>("Container").ShouldBe("contacts");
        result.Details.GetValue<string>("PrimaryKeyField").ShouldBe("id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task UpdateTranslatorKeepsRefusingANullPrimaryKeyWhenThereIsNoFilter()
    {
        var container = CreateContainer("contacts");
        IDictionary<string, object?> record = new Dictionary<string, object?> { ["id"] = null, ["name"] = "Bob" };
        var command = new UpdateCommand<IDictionary<string, object?>>(record);

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("NullPrimaryKeyValue");
    }
}
