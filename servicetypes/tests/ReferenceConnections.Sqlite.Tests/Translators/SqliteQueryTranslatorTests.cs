using Fdw.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Data.Sqlite.Results;
using Moq;
using Shouldly;
using Xunit;

namespace ReferenceConnections.Sqlite.Tests.Translators;

/// <summary>
/// Security-focused coverage for <see cref="SqliteQueryTranslator"/>'s <c>IsValidColumnName</c>
/// guards on the JOIN target, JOIN condition columns, and multi-source projection fields.
/// Every malicious identifier must be rejected fail-loud (a non-success <see cref="IGenericResult{T}"/>
/// carrying <c>QueryTranslationFailedCode</c>) — never emitted into SQL text.
/// </summary>
[Collection(nameof(SqliteTestCollection))]
public sealed class SqliteQueryTranslatorTests
{
    private readonly SqliteQueryTranslator _sut = new();

    private static Mock<IDataContainer> CreateContainer(
        string name = "orders",
        IField[]? fields = null)
    {
        // Why: SqliteDatabasePath is schemaless — only an object name; no database or schema segment.
        var dbPath = new SqliteDatabasePath(name);

        var containerSchema = new Mock<IContainerSchema>();
        containerSchema.Setup(s => s.Fields)
            .Returns(fields ?? new[] { CreateField("id").Object });
        containerSchema.Setup(s => s.GetProjectableFields()).Returns(fields ?? new[] { CreateField("id").Object });

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

    private static Mock<IField> CreateField(string name)
    {
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.Visibility).Returns(FieldVisibilities.ByName("Visible"));
        field.Setup(f => f.IsIdentity).Returns(false);
        field.Setup(f => f.IsComputed).Returns(false);
        return field;
    }

    private static Mock<IQueryCommand> CreateQueryCommandWithJoins(IReadOnlyList<IJoinExpression> joins)
    {
        var cmd = new Mock<IQueryCommand>();
        cmd.Setup(c => c.Filter).Returns((IFilterExpression?)null);
        cmd.Setup(c => c.Projection).Returns((IProjectionExpression?)null);
        cmd.Setup(c => c.Ordering).Returns((IOrderingExpression?)null);
        cmd.Setup(c => c.Paging).Returns((IPagingExpression?)null);
        cmd.Setup(c => c.Joins).Returns(joins);
        return cmd;
    }

    private static Mock<IQueryCommand> CreateQueryCommandWithProjection(IProjectionExpression projection)
    {
        var cmd = new Mock<IQueryCommand>();
        cmd.Setup(c => c.Filter).Returns((IFilterExpression?)null);
        cmd.Setup(c => c.Projection).Returns(projection);
        cmd.Setup(c => c.Ordering).Returns((IOrderingExpression?)null);
        cmd.Setup(c => c.Paging).Returns((IPagingExpression?)null);
        cmd.Setup(c => c.Joins).Returns(new List<IJoinExpression>());
        return cmd;
    }

    // ── JOIN target container name ─────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateAcceptsValidJoinAndProducesJoinedSelectSql()
    {
        var container = CreateContainer("orders");
        var joins = new List<IJoinExpression>
        {
            new JoinExpression
            {
                TargetContainerName = "orderitems",
                JoinType = "INNER",
                JoinConditions = [("OrderId", "OrderId")]
            }
        };

        var result = await _sut.Translate(CreateQueryCommandWithJoins(joins).Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldBe(
            "SELECT \"orders\".\"id\" FROM \"orders\" INNER JOIN \"orderitems\" ON \"orders\".\"OrderId\" = \"orderitems\".\"OrderId\"");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("x; DROP TABLE")]
    [InlineData("a' OR '1'='1")]
    [InlineData("col--")]
    [InlineData("name with space")]
    [InlineData("name\"quote")]
    [InlineData("name;semicolon")]
    public async Task TranslateRejectsSqlInjectionInJoinTargetContainerName(string maliciousTargetName)
    {
        // Why: BuildJoinedSelectStatement validates join.TargetContainerName via IsValidColumnName
        // (SqliteQueryTranslator.cs ~183) BEFORE it is interpolated into the JOIN clause. A hostile
        // target name must never reach SQL text — the translator must fail loud instead.
        var container = CreateContainer("orders");
        var joins = new List<IJoinExpression>
        {
            new JoinExpression
            {
                TargetContainerName = maliciousTargetName,
                JoinType = "INNER",
                JoinConditions = [("OrderId", "OrderId")]
            }
        };

        var result = await _sut.Translate(CreateQueryCommandWithJoins(joins).Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("QueryTranslationFailed");
    }

    // ── JOIN condition columns ──────────────────────────────────────────────

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("x; DROP TABLE")]
    [InlineData("a' OR '1'='1")]
    [InlineData("col--")]
    [InlineData("name with space")]
    public async Task TranslateRejectsSqlInjectionInJoinConditionLeftField(string maliciousLeftField)
    {
        // Why: each join condition's LeftField/RightField is validated via IsValidColumnName
        // (SqliteQueryTranslator.cs ~196) before being quoted into "primary"."Left" = "target"."Right".
        var container = CreateContainer("orders");
        var joins = new List<IJoinExpression>
        {
            new JoinExpression
            {
                TargetContainerName = "orderitems",
                JoinType = "INNER",
                JoinConditions = [(maliciousLeftField, "OrderId")]
            }
        };

        var result = await _sut.Translate(CreateQueryCommandWithJoins(joins).Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("QueryTranslationFailed");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("x; DROP TABLE")]
    [InlineData("a' OR '1'='1")]
    [InlineData("col--")]
    [InlineData("name with space")]
    public async Task TranslateRejectsSqlInjectionInJoinConditionRightField(string maliciousRightField)
    {
        var container = CreateContainer("orders");
        var joins = new List<IJoinExpression>
        {
            new JoinExpression
            {
                TargetContainerName = "orderitems",
                JoinType = "INNER",
                JoinConditions = [("OrderId", maliciousRightField)]
            }
        };

        var result = await _sut.Translate(CreateQueryCommandWithJoins(joins).Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("QueryTranslationFailed");
    }

    // ── Multi-source projection (PropertyName / SourceContainer / Alias) ────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateAcceptsValidMultiSourceProjectionWithAlias()
    {
        var container = CreateContainer("orders");
        var projection = new ProjectionExpression
        {
            Fields =
            [
                new ProjectionField { PropertyName = "id", SourceContainer = "orders" },
                new ProjectionField { PropertyName = "total", SourceContainer = "orderitems", Alias = "itemTotal" }
            ]
        };

        var result = await _sut.Translate(CreateQueryCommandWithProjection(projection).Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldBe(
            "SELECT \"orders\".\"id\", \"orderitems\".\"total\" AS \"itemTotal\" FROM \"orders\"");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("x; DROP TABLE")]
    [InlineData("a' OR '1'='1")]
    [InlineData("col--")]
    [InlineData("name with space")]
    public async Task TranslateRejectsSqlInjectionInProjectionFieldPropertyName(string maliciousPropertyName)
    {
        // Why: in the multi-source projection branch, field.PropertyName is validated via
        // IsValidColumnName (SqliteQueryTranslator.cs ~237) before being quoted into the column list.
        var container = CreateContainer("orders");
        var projection = new ProjectionExpression
        {
            Fields = [new ProjectionField { PropertyName = maliciousPropertyName, SourceContainer = "orders" }]
        };

        var result = await _sut.Translate(CreateQueryCommandWithProjection(projection).Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("QueryTranslationFailed");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("x; DROP TABLE")]
    [InlineData("a' OR '1'='1")]
    [InlineData("col--")]
    [InlineData("name with space")]
    public async Task TranslateRejectsSqlInjectionInProjectionSourceContainer(string maliciousSourceContainer)
    {
        // Why: field.SourceContainer is independently validated via IsValidColumnName
        // (SqliteQueryTranslator.cs ~243) before being quoted as the column's table qualifier.
        var container = CreateContainer("orders");
        var projection = new ProjectionExpression
        {
            Fields = [new ProjectionField { PropertyName = "id", SourceContainer = maliciousSourceContainer }]
        };

        var result = await _sut.Translate(CreateQueryCommandWithProjection(projection).Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("QueryTranslationFailed");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("x; DROP TABLE")]
    [InlineData("a' OR '1'='1")]
    [InlineData("col--")]
    [InlineData("name with space")]
    public async Task TranslateRejectsSqlInjectionInProjectionAlias(string maliciousAlias)
    {
        // Why: field.Alias is independently validated via IsValidColumnName
        // (SqliteQueryTranslator.cs ~254) before being quoted as "col AS alias".
        var container = CreateContainer("orders");
        var projection = new ProjectionExpression
        {
            Fields = [new ProjectionField { PropertyName = "id", SourceContainer = "orders", Alias = maliciousAlias }]
        };

        var result = await _sut.Translate(CreateQueryCommandWithProjection(projection).Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("QueryTranslationFailed");
    }

    // ── Single-source projection (PropertyNames-only path) ──────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateAcceptsValidSinglePropertyNameProjection()
    {
        var container = CreateContainer("orders");
        var projection = new ProjectionExpression
        {
            Fields = [new ProjectionField { PropertyName = "id" }]
        };

        var result = await _sut.Translate(CreateQueryCommandWithProjection(projection).Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldBe("SELECT \"id\" FROM \"orders\"");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("x; DROP TABLE")]
    [InlineData("a' OR '1'='1")]
    [InlineData("col--")]
    [InlineData("name with space")]
    public async Task TranslateRejectsSqlInjectionInSinglePropertyNameProjection(string maliciousPropertyName)
    {
        // Why: when no field carries a SourceContainer, the translator takes the simpler
        // PropertyNames-only branch (SqliteQueryTranslator.cs ~268) which independently
        // re-validates every name via IsValidColumnName before quoting it.
        var container = CreateContainer("orders");
        var projection = new ProjectionExpression
        {
            Fields = [new ProjectionField { PropertyName = maliciousPropertyName }]
        };

        var result = await _sut.Translate(CreateQueryCommandWithProjection(projection).Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("QueryTranslationFailed");
    }

    // ── Container/schema guards resolve their OWN registered ResultCode ─────
    // (not the SqliteDataResultCodes.NotFound sentinel — R3 registered these two
    // [TypeOption]s; R1 had removed the asserting tests to avoid locking in the
    // pre-fix NotFound behavior.)

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateReturnsContainerNotDataContainerCodeWhenContainerIsPlainStorageContainer()
    {
        // Why: ContainerNotDataContainerCode is a registered [TypeOption] on SqliteDataResultCodes
        // (Results/ContainerNotDataContainerCode.cs). Before it was registered, ByName("ContainerNotDataContainer")
        // silently resolved to the NotFound sentinel instead of the intended code. Assert the real code
        // identity here so a regression back to NotFound is caught.
        var dbPath = new SqliteDatabasePath("orders");
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(Array.Empty<IField>());
        schema.Setup(s => s.GetProjectableFields()).Returns(Array.Empty<IField>());

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("orders");
        container.Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(schema.Object);

        var command = CreateQueryCommandWithJoins(new List<IJoinExpression>());

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("ContainerNotDataContainer");
        result.Code.ShouldNotBe(SqliteDataResultCodes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateReturnsNoFieldsToProjectCodeWhenNoProjectionAndNoSchemaFields()
    {
        // Why: NoFieldsToProjectCode is a registered [TypeOption] on SqliteDataResultCodes
        // (Results/NoFieldsToProjectCode.cs) guarding the SELECT * emission path. Before it was
        // registered, ByName("NoFieldsToProject") silently resolved to the NotFound sentinel.
        // Assert the real code identity so a regression back to NotFound is caught.
        var container = CreateContainer("orders", fields: Array.Empty<IField>());
        var command = CreateQueryCommandWithJoins(new List<IJoinExpression>());

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("NoFieldsToProject");
        result.Code.ShouldNotBe(SqliteDataResultCodes.NotFound);
    }
}
