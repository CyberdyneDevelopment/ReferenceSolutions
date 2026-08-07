using Fdw.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Moq;
using Shouldly;
using Xunit;

namespace ReferenceConnections.Sqlite.Tests.Translators;

/// <summary>
/// Verifies SQLite-specific SQL output: double-quote identifiers, LIMIT/OFFSET paging, FALSE empty-IN.
/// </summary>
[Collection(nameof(SqliteTestCollection))]
public sealed class SqliteTranslatorTests
{
    private readonly SqliteQueryTranslator _query = new();
    private readonly SqliteFindTranslator _find = new();

    private static Mock<IDataContainer> CreateContainer(
        string name = "orders",
        IField[]? fields = null)
    {
        // Why: SqliteDatabasePath is schemaless — only an object name; no database or schema segment.
        var dbPath = new SqliteDatabasePath(name);

        var containerSchema = new Mock<IContainerSchema>();
        containerSchema.Setup(s => s.Fields)
            .Returns(fields ?? new[] { CreateField("id").Object, CreateField("amount").Object });

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.As<IStorageContainer>().Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(containerSchema.Object);
        container.Setup(c => c.Metadata).Returns(new Dictionary<string, object>(StringComparer.Ordinal));
        container.Setup(c => c.Keys).Returns(new List<IContainerKey>());
        container.Setup(c => c.ReferencingKeys)
            .Returns(GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success([]));

        return container;
    }

    private static Mock<IField> CreateField(string name, bool isIdentity = false, bool isComputed = false)
    {
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.IsIdentity).Returns(isIdentity);
        field.Setup(f => f.IsComputed).Returns(isComputed);
        return field;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void DialectQuotesIdentifiersWithDoubleQuotes()
    {
        SqliteDialect.Instance.QuoteIdentifier("my_col").ShouldBe("\"my_col\"");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void DialectBuildsPagingAsLimitOffset()
    {
        var paging = new Mock<IPagingExpression>();
        paging.Setup(p => p.Skip).Returns(20);
        paging.Setup(p => p.Take).Returns(10);

        SqliteDialect.Instance.BuildPagingClause(paging.Object).ShouldBe("LIMIT 10 OFFSET 20");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void DialectAlwaysFalsePredicateIsFalse()
    {
        SqliteDialect.Instance.AlwaysFalsePredicate.ShouldBe("FALSE");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task QueryTranslatorProducesDoubleQuotedSelectFrom()
    {
        var container = CreateContainer("customers",
            new[] { CreateField("id").Object, CreateField("name").Object });

        var cmd = new Mock<IQueryCommand>();
        cmd.Setup(c => c.Filter).Returns((IFilterExpression?)null);
        cmd.Setup(c => c.Projection).Returns((IProjectionExpression?)null);
        cmd.Setup(c => c.Ordering).Returns((IOrderingExpression?)null);
        cmd.Setup(c => c.Paging).Returns((IPagingExpression?)null);
        cmd.Setup(c => c.Joins).Returns(new List<IJoinExpression>());

        var result = await _query.Translate(cmd.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        // SQLite uses double-quote identifiers, no schema prefix.
        result.Value!.CommandText.ShouldBe("SELECT \"id\", \"name\" FROM \"customers\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task QueryTranslatorAppendsPagingAsLimitOffset()
    {
        var container = CreateContainer("orders",
            new[] { CreateField("id").Object, CreateField("amount").Object });

        var paging = new Mock<IPagingExpression>();
        paging.Setup(p => p.Skip).Returns(10);
        paging.Setup(p => p.Take).Returns(5);

        var sortDirection = SortDirections.ByName("Ascending");
        var orderedField = new Mock<IOrderedField>();
        orderedField.Setup(f => f.PropertyName).Returns("id");
        orderedField.Setup(f => f.Direction).Returns(sortDirection);

        var ordering = new Mock<IOrderingExpression>();
        ordering.Setup(o => o.OrderedFields).Returns(new List<IOrderedField> { orderedField.Object });

        var cmd = new Mock<IQueryCommand>();
        cmd.Setup(c => c.Filter).Returns((IFilterExpression?)null);
        cmd.Setup(c => c.Projection).Returns((IProjectionExpression?)null);
        cmd.Setup(c => c.Ordering).Returns(ordering.Object);
        cmd.Setup(c => c.Paging).Returns(paging.Object);
        cmd.Setup(c => c.Joins).Returns(new List<IJoinExpression>());

        var result = await _query.Translate(cmd.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("LIMIT 5 OFFSET 10");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task FindTranslatorEmitsFalseWhenNoSearchableFields()
    {
        // Why: when the container has no fields and FieldNames is null,
        // searchColumns is empty — the translator must emit WHERE FALSE rather than
        // an invalid empty OR clause. This exercises AlwaysFalsePredicate = "FALSE".
        var container = CreateContainer("products", fields: []);

        var cmd = new FindCommand<object>
        {
            SearchTerm = "test",
            FieldNames = null,
            CaseSensitive = false
        };

        var result = await _find.Translate(cmd, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE FALSE");
    }

}
