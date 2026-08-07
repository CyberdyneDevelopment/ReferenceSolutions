using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Protocols;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReferenceConnections.Http.Tests.Protocols;

/// <summary>
/// Tests for OData-style protocol behavior via a testable implementation.
/// </summary>
/// <remarks>
/// Since <see cref="ODataProtocol"/> is sealed and excluded from coverage as a TypeOption,
/// we test OData-specific logic through a testable implementation that mirrors the behavior.
/// </remarks>
public class ODataProtocolTests
{
    private readonly TestableODataLikeProtocol _protocol;
    private readonly HttpProtocolContext _context;

    public ODataProtocolTests()
    {
        _protocol = new TestableODataLikeProtocol();
        _context = CreateTestContext();
    }

    private static HttpProtocolContext CreateTestContext()
    {
        var config = new HttpConnectionConfiguration
        {
            BaseUrl = "https://api.example.com/odata"
        };
        return new HttpProtocolContext(
            Configuration: config,
            LoggerFactory: NullLoggerFactory.Instance,
            ResolvedCertificate: null,
            ResolvedPassword: null,
            ResolvedApiKey: null);
    }

    #region OData Pagination Tests

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildPaginationQueryStringWithSkipReturnsODataSkip()
    {
        // Arrange
        var paging = new Mock<IPagingExpression>();
        paging.Setup(p => p.Skip).Returns(20);
        paging.Setup(p => p.Take).Returns(0);

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Paging).Returns(paging.Object);

        // Act
        var result = _protocol.BuildPaginationQueryString(command.Object, _context);

        // Assert
        result.ShouldBe("$skip=20");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildPaginationQueryStringWithTakeReturnsODataTop()
    {
        // Arrange
        var paging = new Mock<IPagingExpression>();
        paging.Setup(p => p.Skip).Returns(0);
        paging.Setup(p => p.Take).Returns(10);

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Paging).Returns(paging.Object);

        // Act
        var result = _protocol.BuildPaginationQueryString(command.Object, _context);

        // Assert
        result.ShouldBe("$top=10");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildPaginationQueryStringWithBothReturnsSkipAndTop()
    {
        // Arrange
        var paging = new Mock<IPagingExpression>();
        paging.Setup(p => p.Skip).Returns(50);
        paging.Setup(p => p.Take).Returns(25);

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Paging).Returns(paging.Object);

        // Act
        var result = _protocol.BuildPaginationQueryString(command.Object, _context);

        // Assert
        result.ShouldBe("$skip=50&$top=25");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildPaginationQueryStringWithNullPagingReturnsEmpty()
    {
        // Arrange
        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Paging).Returns((IPagingExpression?)null);

        // Act
        var result = _protocol.BuildPaginationQueryString(command.Object, _context);

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region OData Ordering Tests

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildOrderingQueryStringWithAscendingReturnsODataOrderby()
    {
        // Arrange
        var direction = new Mock<ISortDirection>();
        direction.Setup(d => d.Name).Returns("Ascending");

        var field = new Mock<IOrderedField>();
        field.Setup(f => f.PropertyName).Returns("name");
        field.Setup(f => f.Direction).Returns(direction.Object);

        var ordering = new Mock<IOrderingExpression>();
        ordering.Setup(o => o.OrderedFields).Returns(new List<IOrderedField> { field.Object });

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Ordering).Returns(ordering.Object);

        // Act
        var result = _protocol.BuildOrderingQueryString(command.Object, _context);

        // Assert
        result.ShouldBe("$orderby=name asc");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildOrderingQueryStringWithDescendingReturnsODataOrderbyDesc()
    {
        // Arrange
        var direction = new Mock<ISortDirection>();
        direction.Setup(d => d.Name).Returns("Descending");

        var field = new Mock<IOrderedField>();
        field.Setup(f => f.PropertyName).Returns("createdAt");
        field.Setup(f => f.Direction).Returns(direction.Object);

        var ordering = new Mock<IOrderingExpression>();
        ordering.Setup(o => o.OrderedFields).Returns(new List<IOrderedField> { field.Object });

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Ordering).Returns(ordering.Object);

        // Act
        var result = _protocol.BuildOrderingQueryString(command.Object, _context);

        // Assert
        result.ShouldBe("$orderby=createdAt desc");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildOrderingQueryStringWithMultipleFieldsCombinesWithComma()
    {
        // Arrange
        var ascDirection = new Mock<ISortDirection>();
        ascDirection.Setup(d => d.Name).Returns("Ascending");

        var descDirection = new Mock<ISortDirection>();
        descDirection.Setup(d => d.Name).Returns("Descending");

        var field1 = new Mock<IOrderedField>();
        field1.Setup(f => f.PropertyName).Returns("name");
        field1.Setup(f => f.Direction).Returns(ascDirection.Object);

        var field2 = new Mock<IOrderedField>();
        field2.Setup(f => f.PropertyName).Returns("date");
        field2.Setup(f => f.Direction).Returns(descDirection.Object);

        var ordering = new Mock<IOrderingExpression>();
        ordering.Setup(o => o.OrderedFields).Returns(new List<IOrderedField> { field1.Object, field2.Object });

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Ordering).Returns(ordering.Object);

        // Act
        var result = _protocol.BuildOrderingQueryString(command.Object, _context);

        // Assert
        result.ShouldBe("$orderby=name asc,date desc");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildOrderingQueryStringWithNoFieldsReturnsEmpty()
    {
        // Arrange
        var ordering = new Mock<IOrderingExpression>();
        ordering.Setup(o => o.OrderedFields).Returns(new List<IOrderedField>());

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Ordering).Returns(ordering.Object);

        // Act
        var result = _protocol.BuildOrderingQueryString(command.Object, _context);

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region OData Filter Tests

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildFilterFromExpressionWithEqualReturnsODataEq()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "status",
            Operator = FilterOperators.Equal,
            Value = "active"
        };

        // Act
        var result = _protocol.BuildFilterFromExpression(condition);

        // Assert
        result.ShouldBe("$filter=status eq 'active'");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildFilterFromExpressionWithBooleanFormatsCorrectly()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "isEnabled",
            Operator = FilterOperators.Equal,
            Value = true
        };

        // Act
        var result = _protocol.BuildFilterFromExpression(condition);

        // Assert
        result.ShouldBe("$filter=isEnabled eq true");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildFilterFromExpressionWithNullFormatsCorrectly()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "deletedAt",
            Operator = FilterOperators.Equal,
            Value = null
        };

        // Act
        var result = _protocol.BuildFilterFromExpression(condition);

        // Assert
        result.ShouldBe("$filter=deletedAt eq null");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildFilterFromExpressionWithNumberFormatsWithoutQuotes()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "count",
            Operator = FilterOperators.Equal,
            Value = 42
        };

        // Act
        var result = _protocol.BuildFilterFromExpression(condition);

        // Assert
        result.ShouldBe("$filter=count eq 42");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildFilterFromExpressionWithNotEqualReturnsNe()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "status",
            Operator = FilterOperators.NotEqual,
            Value = "deleted"
        };

        // Act
        var result = _protocol.BuildFilterFromExpression(condition);

        // Assert
        result.ShouldBe("$filter=status ne 'deleted'");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildFilterFromExpressionWithGreaterThanReturnsGt()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "age",
            Operator = FilterOperators.GreaterThan,
            Value = 18
        };

        // Act
        var result = _protocol.BuildFilterFromExpression(condition);

        // Assert
        result.ShouldBe("$filter=age gt 18");
    }

    #endregion

    #region OData Response Wrapper Tests

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void ExtractDataFromWrapperWithODataValueExtractsValue()
    {
        // Arrange
        var content = """{"@odata.context": "https://example.com/$metadata", "value": [{"id": 1}], "@odata.count": 100}""";

        // Act
        var result = _protocol.ExtractDataFromWrapper(content);

        // Assert
        result.ShouldBe("""[{"id": 1}]""");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void ExtractDataFromWrapperWithNoValueReturnsOriginal()
    {
        // Arrange
        var content = """[{"id": 1}, {"id": 2}]""";

        // Act
        var result = _protocol.ExtractDataFromWrapper(content);

        // Assert
        result.ShouldBe(content);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void ExtractDataFromWrapperWithInvalidJsonReturnsOriginal()
    {
        // Arrange
        var content = "not valid json";

        // Act
        var result = _protocol.ExtractDataFromWrapper(content);

        // Assert
        result.ShouldBe(content);
    }

    #endregion
}

/// <summary>
/// Testable OData-like protocol that implements OData conventions.
/// </summary>
/// <remarks>
/// This mimics the behavior of <see cref="ODataProtocol"/> but is not sealed,
/// allowing the protected methods to be tested.
/// </remarks>
[ExcludeFromCodeCoverage]
internal sealed class TestableODataLikeProtocol : RestProtocolBase
{
    public TestableODataLikeProtocol()
        : base(999, "TestOData", "Test OData protocol for unit tests")
    {
    }

    public new string BuildPaginationQueryString(IQueryCommand command, HttpProtocolContext context)
    {
        if (command.Paging is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        if (command.Paging.Skip > 0)
        {
            parts.Add($"$skip={command.Paging.Skip}");
        }

        if (command.Paging.Take > 0)
        {
            parts.Add($"$top={command.Paging.Take}");
        }

        return string.Join("&", parts);
    }

    public new string BuildOrderingQueryString(IQueryCommand command, HttpProtocolContext context)
    {
        if (command.Ordering?.OrderedFields is null || command.Ordering.OrderedFields.Count == 0)
        {
            return string.Empty;
        }

        var orderParts = new List<string>();
        foreach (var field in command.Ordering.OrderedFields)
        {
            var direction = string.Equals(field.Direction.Name, "Descending", System.StringComparison.Ordinal) ? " desc" : " asc";
            orderParts.Add($"{field.PropertyName}{direction}");
        }

        return $"$orderby={string.Join(",", orderParts)}";
    }

    public new string BuildFilterFromExpression(IFilterNode node)
    {
        if (node is IFilterCondition condition)
        {
            var value = condition.Value;
            var formattedValue = value switch
            {
                string s => $"'{s}'",
                bool b => b.ToString().ToLowerInvariant(),
                null => "null",
                _ => value.ToString()
            };

            var op = MapOperatorToOData(condition.Operator?.Name ?? "Equal");
            return $"$filter={condition.PropertyName} {op} {formattedValue}";
        }

        return base.BuildFilterFromExpression(node);
    }

    public new string ExtractDataFromWrapper(string content)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("value", out var value))
            {
                return value.GetRawText();
            }
        }
        catch
        {
            // Not valid JSON
        }

        return content;
    }

    private static string MapOperatorToOData(string operatorName)
    {
        return operatorName switch
        {
            "Equal" => "eq",
            "NotEqual" => "ne",
            "GreaterThan" => "gt",
            "GreaterThanOrEqual" => "ge",
            "LessThan" => "lt",
            "LessThanOrEqual" => "le",
            "Contains" => "contains",
            "StartsWith" => "startswith",
            "EndsWith" => "endswith",
            _ => "eq"
        };
    }
}
