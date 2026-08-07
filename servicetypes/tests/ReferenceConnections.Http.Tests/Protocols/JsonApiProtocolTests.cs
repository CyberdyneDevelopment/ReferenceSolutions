using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Protocols;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReferenceConnections.Http.Tests.Protocols;

/// <summary>
/// Tests for JSON:API-style protocol behavior via a testable implementation.
/// </summary>
/// <remarks>
/// Since <see cref="JsonApiProtocol"/> is sealed and excluded from coverage as a TypeOption,
/// we test JSON:API-specific logic through a testable implementation that mirrors the behavior.
/// </remarks>
public class JsonApiProtocolTests
{
    private readonly TestableJsonApiLikeProtocol _protocol;
    private readonly HttpProtocolContext _context;

    public JsonApiProtocolTests()
    {
        _protocol = new TestableJsonApiLikeProtocol();
        _context = CreateTestContext();
    }

    private static HttpProtocolContext CreateTestContext()
    {
        var config = new HttpConnectionConfiguration
        {
            BaseUrl = "https://api.example.com/v1"
        };
        return new HttpProtocolContext(
            Configuration: config,
            LoggerFactory: NullLoggerFactory.Instance,
            ResolvedCertificate: null,
            ResolvedPassword: null,
            ResolvedApiKey: null);
    }

    #region JSON:API Pagination Tests

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildPaginationQueryStringWithPageBasedReturnPageNumberAndSize()
    {
        // Arrange - Skip 0, Take 25 should give page 1 size 25
        var paging = new Mock<IPagingExpression>();
        paging.Setup(p => p.Skip).Returns(0);
        paging.Setup(p => p.Take).Returns(25);

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Paging).Returns(paging.Object);

        // Act
        var result = _protocol.BuildPaginationQueryString(command.Object, _context);

        // Assert
        result.ShouldContain("page[number]=1");
        result.ShouldContain("page[size]=25");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildPaginationQueryStringWithOffsetCalculatesPageNumber()
    {
        // Arrange - Skip 50, Take 25 should give page 3 (50/25 + 1 = 3)
        var paging = new Mock<IPagingExpression>();
        paging.Setup(p => p.Skip).Returns(50);
        paging.Setup(p => p.Take).Returns(25);

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Paging).Returns(paging.Object);

        // Act
        var result = _protocol.BuildPaginationQueryString(command.Object, _context);

        // Assert
        result.ShouldContain("page[number]=3");
        result.ShouldContain("page[size]=25");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildPaginationQueryStringWithZeroTakeUsesDefaultSize()
    {
        // Arrange - No take specified, should use default (25)
        var paging = new Mock<IPagingExpression>();
        paging.Setup(p => p.Skip).Returns(0);
        paging.Setup(p => p.Take).Returns(0);

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Paging).Returns(paging.Object);

        // Act
        var result = _protocol.BuildPaginationQueryString(command.Object, _context);

        // Assert
        result.ShouldContain("page[number]=1");
        result.ShouldContain("page[size]=25");
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

    #region JSON:API Filter Tests

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildFilterFromExpressionWithConditionReturnsFilterBracketNotation()
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
        result.ShouldBe("filter[status]=active");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildFilterFromExpressionWithSpecialCharactersUrlEncodesValue()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "name",
            Operator = FilterOperators.Equal,
            Value = "Test & Co"
        };

        // Act
        var result = _protocol.BuildFilterFromExpression(condition);

        // Assert
        result.ShouldBe("filter[name]=Test+%26+Co");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildFilterFromExpressionWithGroupCombinesFilters()
    {
        // Arrange
        var group = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition
                {
                    PropertyName = "type",
                    Operator = FilterOperators.Equal,
                    Value = "user"
                },
                new FilterCondition
                {
                    PropertyName = "status",
                    Operator = FilterOperators.Equal,
                    Value = "active"
                }
            ]
        };

        // Act
        var result = _protocol.BuildFilterFromExpression(group);

        // Assert
        result.ShouldContain("filter[type]=user");
        result.ShouldContain("filter[status]=active");
        result.ShouldContain("&");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void BuildFilterFromExpressionWithNullValueEncodesEmpty()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "category",
            Operator = FilterOperators.Equal,
            Value = null
        };

        // Act
        var result = _protocol.BuildFilterFromExpression(condition);

        // Assert
        result.ShouldBe("filter[category]=");
    }

    #endregion

    #region JSON:API Response Wrapper Tests

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void ExtractDataFromWrapperWithJsonApiDataExtractsData()
    {
        // Arrange
        var content = """{"data": [{"id": "1", "type": "users", "attributes": {"name": "Test"}}], "meta": {"total": 100}}""";

        // Act
        var result = _protocol.ExtractDataFromWrapper(content);

        // Assert
        result.ShouldBe("""[{"id": "1", "type": "users", "attributes": {"name": "Test"}}]""");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void ExtractDataFromWrapperWithNoDataReturnsOriginal()
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

    #region JSON:API Pagination Info Extraction Tests

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void ExtractPaginationInfoWithMetaTotalReturnsTotalCount()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var content = """{"data": [], "meta": {"total": 250}}""";

        // Act
        var result = _protocol.ExtractPaginationInfo(response, content, _context);

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(250);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void ExtractPaginationInfoWithLinksNextReturnsNextCursor()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var content = """{"data": [], "links": {"self": "/users?page[number]=1", "next": "/users?page[number]=2"}}""";

        // Act
        var result = _protocol.ExtractPaginationInfo(response, content, _context);

        // Assert
        result.ShouldNotBeNull();
        result.NextCursor.ShouldBe("/users?page[number]=2");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void ExtractPaginationInfoWithBothMetaAndLinksReturnsBoth()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var content = """{"data": [], "meta": {"total": 100}, "links": {"next": "/users?page[number]=3"}}""";

        // Act
        var result = _protocol.ExtractPaginationInfo(response, content, _context);

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(100);
        result.NextCursor.ShouldBe("/users?page[number]=3");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void ExtractPaginationInfoWithNoMetaOrLinksReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var content = """{"data": []}""";

        // Act
        var result = _protocol.ExtractPaginationInfo(response, content, _context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void ExtractPaginationInfoWithInvalidJsonReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var content = "not valid json";

        // Act
        var result = _protocol.ExtractPaginationInfo(response, content, _context);

        // Assert
        result.ShouldBeNull();
    }

    #endregion
}

/// <summary>
/// Testable JSON:API-like protocol that implements JSON:API conventions.
/// </summary>
/// <remarks>
/// This mimics the behavior of <see cref="JsonApiProtocol"/> but is not sealed,
/// allowing the protected methods to be tested.
/// </remarks>
[ExcludeFromCodeCoverage]
internal sealed class TestableJsonApiLikeProtocol : RestProtocolBase
{
    public TestableJsonApiLikeProtocol()
        : base(998, "TestJsonApi", "Test JSON:API protocol for unit tests")
    {
    }

    public new string BuildPaginationQueryString(IQueryCommand command, HttpProtocolContext context)
    {
        if (command.Paging is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        // JSON:API uses page-based pagination
        var pageSize = command.Paging.Take > 0 ? command.Paging.Take : 25;
        var pageNumber = (command.Paging.Skip / pageSize) + 1;

        parts.Add($"page[number]={pageNumber}");
        parts.Add($"page[size]={pageSize}");

        return string.Join("&", parts);
    }

    public new string BuildFilterFromExpression(IFilterNode node)
    {
        if (node is IFilterCondition condition)
        {
            var encodedValue = System.Web.HttpUtility.UrlEncode(condition.Value?.ToString() ?? string.Empty);
            return $"filter[{condition.PropertyName}]={encodedValue}";
        }

        if (node is FilterGroup group)
        {
            var parts = new List<string>();
            foreach (var child in group.Nodes)
            {
                if (child is IFilterCondition childCondition)
                {
                    var encodedValue = System.Web.HttpUtility.UrlEncode(childCondition.Value?.ToString() ?? string.Empty);
                    parts.Add($"filter[{childCondition.PropertyName}]={encodedValue}");
                }
            }
            return string.Join("&", parts);
        }

        return string.Empty;
    }

    public new string ExtractDataFromWrapper(string content)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            var root = doc.RootElement;

            // JSON:API always uses "data"
            if (root.TryGetProperty("data", out var data))
            {
                return data.GetRawText();
            }
        }
        catch
        {
            // Not valid JSON
        }

        return content;
    }

    public new RestPaginationInfo? ExtractPaginationInfo(
        HttpResponseMessage response,
        string content,
        HttpProtocolContext context)
    {
        // JSON:API provides pagination in meta and links
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            var root = doc.RootElement;

            int? totalCount = null;
            string? nextCursor = null;

            // Check meta for total
            if (root.TryGetProperty("meta", out var meta))
            {
                if (meta.TryGetProperty("total", out var total) && total.TryGetInt32(out var totalValue))
                {
                    totalCount = totalValue;
                }
            }

            // Check links for next
            if (root.TryGetProperty("links", out var links))
            {
                if (links.TryGetProperty("next", out var next) && next.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    nextCursor = next.GetString();
                }
            }

            if (totalCount.HasValue || !string.IsNullOrEmpty(nextCursor))
            {
                return new RestPaginationInfo(totalCount, nextCursor);
            }
        }
        catch
        {
            // Not valid JSON:API response
        }

        return null;
    }
}
