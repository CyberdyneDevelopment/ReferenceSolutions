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
/// Verifies DELETE SQL shape produced by <see cref="SqliteDeleteTranslator"/>:
/// double-quote identifiers, schemaless table name, fail-loud on missing filter.
/// </summary>
[Collection(nameof(SqliteTestCollection))]
public sealed class SqliteDeleteTranslatorTests
{
    private readonly SqliteDeleteTranslator _translator = new();

    private static Mock<IDataContainer> CreateContainer(string tableName = "orders")
    {
        var dbPath = new SqliteDatabasePath(tableName);
        var idField = CreateField("id", isIdentity: true);
        var totalField = CreateField("total");

        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields)
            .Returns(new IField[] { idField.Object, totalField.Object });

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
    public async Task DeleteTranslatorProducesDoubleQuotedDeleteFrom()
    {
        var container = CreateContainer("orders");
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "id",
                Operator = FilterOperators.ByName("Equal"),
                Value = 42L
            }
        };
        var command = new DeleteCommand { Filter = filter };

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldStartWith("DELETE FROM \"orders\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task DeleteTranslatorWithFilterProducesWhereClause()
    {
        var container = CreateContainer("orders");
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "id",
                Operator = FilterOperators.ByName("Equal"),
                Value = 42L
            }
        };
        var command = new DeleteCommand { Filter = filter };

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE");
        result.Value.CommandText.ShouldContain("\"id\"");
        result.Value.Parameters.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task DeleteTranslatorWithoutFilterReturnsStructuredFailure()
    {
        // Fail-loud: a DELETE without a filter must never succeed — it would wipe the table.
        var container = CreateContainer("orders");
        var command = new DeleteCommand { Filter = null };

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataGateway")]
    public async Task DeleteTranslatorWithInvalidContainerPathReturnsStructuredFailure()
    {
        var badPath = new Mock<IPath>();
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(System.Array.Empty<IField>());

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("orders");
        container.As<IStorageContainer>().Setup(c => c.Path).Returns(badPath.Object);
        container.Setup(c => c.Schema).Returns(schema.Object);

        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "id",
                Operator = FilterOperators.ByName("Equal"),
                Value = 1L
            }
        };
        var command = new DeleteCommand { Filter = filter };

        var result = await _translator.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }
}
