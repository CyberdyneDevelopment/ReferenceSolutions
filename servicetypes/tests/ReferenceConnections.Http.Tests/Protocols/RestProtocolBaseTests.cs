using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Http;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Protocols;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReferenceConnections.Http.Tests.Protocols;

public class RestProtocolBaseTests
{
    private readonly TestableRestProtocol _protocol;
    private readonly HttpProtocolContext _context;

    public RestProtocolBaseTests()
    {
        _protocol = new TestableRestProtocol();
        _context = CreateTestContext();
    }

    private static HttpProtocolContext CreateTestContext()
    {
        var config = new HttpConnectionConfiguration
        {
            BaseUrl = "https://api.example.com"
        };
        return new HttpProtocolContext(
            Configuration: config,
            LoggerFactory: NullLoggerFactory.Instance,
            ResolvedCertificate: null,
            ResolvedPassword: null,
            ResolvedApiKey: null);
    }

    #region BuildFilterFromExpression Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildFilterFromExpressionWithSingleConditionReturnsKeyValuePair()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "name",
            Operator = FilterOperators.Equal,
            Value = "test"
        };

        // Act
        var result = _protocol.BuildFilterFromExpression(condition);

        // Assert
        result.ShouldBe("name=test");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildFilterFromExpressionWithSpecialCharactersUrlEncodesValue()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "query",
            Operator = FilterOperators.Equal,
            Value = "hello world&foo=bar"
        };

        // Act
        var result = _protocol.BuildFilterFromExpression(condition);

        // Assert
        result.ShouldBe("query=hello+world%26foo%3dbar");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildFilterFromExpressionWithNullValueReturnsEmptyString()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "field",
            Operator = FilterOperators.Equal,
            Value = null
        };

        // Act
        var result = _protocol.BuildFilterFromExpression(condition);

        // Assert
        result.ShouldBe("field=");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildFilterFromExpressionWithFilterGroupCombinesConditions()
    {
        // Arrange
        var group = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition
                {
                    PropertyName = "status",
                    Operator = FilterOperators.Equal,
                    Value = "active"
                },
                new FilterCondition
                {
                    PropertyName = "type",
                    Operator = FilterOperators.Equal,
                    Value = "user"
                }
            ]
        };

        // Act
        var result = _protocol.BuildFilterFromExpression(group);

        // Assert
        result.ShouldBe("status=active&type=user");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildFilterFromExpressionWithNestedGroupsFlattensCorrectly()
    {
        // Arrange
        var group = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition
                {
                    PropertyName = "active",
                    Operator = FilterOperators.Equal,
                    Value = "true"
                },
                new FilterGroup
                {
                    Operator = LogicalOperator.Or,
                    Nodes =
                    [
                        new FilterCondition
                        {
                            PropertyName = "role",
                            Operator = FilterOperators.Equal,
                            Value = "admin"
                        },
                        new FilterCondition
                        {
                            PropertyName = "role",
                            Operator = FilterOperators.Equal,
                            Value = "editor"
                        }
                    ]
                }
            ]
        };

        // Act
        var result = _protocol.BuildFilterFromExpression(group);

        // Assert
        result.ShouldContain("active=true");
        result.ShouldContain("role=admin");
        result.ShouldContain("role=editor");
    }

    #endregion

    #region BuildPaginationQueryString Tests

    [Fact]
    [Trait("Priority", "P1")]
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

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildPaginationQueryStringWithSkipOnlyReturnsOffset()
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
        result.ShouldBe("offset=20");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildPaginationQueryStringWithTakeOnlyReturnsLimit()
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
        result.ShouldBe("limit=10");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildPaginationQueryStringWithBothSkipAndTakeReturnsBoth()
    {
        // Arrange
        var paging = new Mock<IPagingExpression>();
        paging.Setup(p => p.Skip).Returns(20);
        paging.Setup(p => p.Take).Returns(10);

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Paging).Returns(paging.Object);

        // Act
        var result = _protocol.BuildPaginationQueryString(command.Object, _context);

        // Assert
        result.ShouldBe("offset=20&limit=10");
    }

    #endregion

    #region BuildOrderingQueryString Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildOrderingQueryStringWithNullOrderingReturnsEmpty()
    {
        // Arrange
        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Ordering).Returns((IOrderingExpression?)null);

        // Act
        var result = _protocol.BuildOrderingQueryString(command.Object, _context);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildOrderingQueryStringWithEmptyFieldsReturnsEmpty()
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

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildOrderingQueryStringWithAscendingFieldReturnsFieldName()
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
        result.ShouldBe("sort=name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildOrderingQueryStringWithDescendingFieldReturnsMinusPrefix()
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
        result.ShouldBe("sort=-createdAt");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildOrderingQueryStringWithMultipleFieldsCombinesThem()
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
        field2.Setup(f => f.PropertyName).Returns("createdAt");
        field2.Setup(f => f.Direction).Returns(descDirection.Object);

        var ordering = new Mock<IOrderingExpression>();
        ordering.Setup(o => o.OrderedFields).Returns(new List<IOrderedField> { field1.Object, field2.Object });

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Ordering).Returns(ordering.Object);

        // Act
        var result = _protocol.BuildOrderingQueryString(command.Object, _context);

        // Assert
        result.ShouldBe("sort=name,-createdAt");
    }

    #endregion

    #region ParseErrorResponse Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ParseErrorResponseWithSimpleErrorPropertyReturnsMessage()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var content = """{"error": "Invalid input"}""";

        // Act
        var result = _protocol.ParseErrorResponse(response, content, _context);

        // Assert
        result.ShouldBe("Invalid input");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ParseErrorResponseWithNestedErrorMessageReturnsMessage()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var content = """{"error": {"message": "Validation failed", "code": "VALIDATION_ERROR"}}""";

        // Act
        var result = _protocol.ParseErrorResponse(response, content, _context);

        // Assert
        result.ShouldBe("Validation failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ParseErrorResponseWithMessagePropertyReturnsMessage()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var content = """{"message": "Internal server error occurred"}""";

        // Act
        var result = _protocol.ParseErrorResponse(response, content, _context);

        // Assert
        result.ShouldBe("Internal server error occurred");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ParseErrorResponseWithRfc7807DetailReturnsDetail()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var content = """{"type": "about:blank", "title": "Not Found", "detail": "User with ID 123 not found"}""";

        // Act
        var result = _protocol.ParseErrorResponse(response, content, _context);

        // Assert
        result.ShouldBe("User with ID 123 not found");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ParseErrorResponseWithRfc7807TitleOnlyReturnsTitle()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
        var content = """{"type": "about:blank", "title": "Access Denied"}""";

        // Act
        var result = _protocol.ParseErrorResponse(response, content, _context);

        // Assert
        result.ShouldBe("Access Denied");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ParseErrorResponseWithInvalidJsonReturnsStatusCodeMessage()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            ReasonPhrase = "Bad Gateway"
        };
        var content = "Not valid JSON";

        // Act
        var result = _protocol.ParseErrorResponse(response, content, _context);

        // Assert
        result.ShouldBe("HTTP 502: Bad Gateway");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ParseErrorResponseWithEmptyContentReturnsStatusCodeMessage()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            ReasonPhrase = "Service Unavailable"
        };
        var content = "";

        // Act
        var result = _protocol.ParseErrorResponse(response, content, _context);

        // Assert
        result.ShouldBe("HTTP 503: Service Unavailable");
    }

    #endregion

    #region ExtractDataFromWrapper Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExtractDataFromWrapperWithDataPropertyExtractsData()
    {
        // Arrange
        var content = """{"data": [{"id": 1, "name": "Test"}], "meta": {"total": 100}}""";

        // Act
        var result = _protocol.ExtractDataFromWrapper(content);

        // Assert
        result.ShouldBe("""[{"id": 1, "name": "Test"}]""");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExtractDataFromWrapperWithResultsPropertyExtractsResults()
    {
        // Arrange
        var content = """{"results": [{"id": 1}], "count": 1}""";

        // Act
        var result = _protocol.ExtractDataFromWrapper(content);

        // Assert
        result.ShouldBe("""[{"id": 1}]""");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExtractDataFromWrapperWithItemsPropertyExtractsItems()
    {
        // Arrange
        var content = """{"items": [{"id": 1}], "total": 1}""";

        // Act
        var result = _protocol.ExtractDataFromWrapper(content);

        // Assert
        result.ShouldBe("""[{"id": 1}]""");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExtractDataFromWrapperWithValuePropertyExtractsValue()
    {
        // Arrange
        var content = """{"value": [{"id": 1}], "@odata.count": 1}""";

        // Act
        var result = _protocol.ExtractDataFromWrapper(content);

        // Assert
        result.ShouldBe("""[{"id": 1}]""");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExtractDataFromWrapperWithNoWrapperReturnsOriginal()
    {
        // Arrange
        var content = """[{"id": 1, "name": "Test"}]""";

        // Act
        var result = _protocol.ExtractDataFromWrapper(content);

        // Assert
        result.ShouldBe(content);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExtractDataFromWrapperWithInvalidJsonReturnsOriginal()
    {
        // Arrange
        var content = "Not JSON";

        // Act
        var result = _protocol.ExtractDataFromWrapper(content);

        // Assert
        result.ShouldBe(content);
    }

    #endregion

    #region ExtractPaginationInfo Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExtractPaginationInfoWithTotalCountHeaderReturnsCount()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-Total-Count", "100");
        var content = "[]";

        // Act
        var result = _protocol.ExtractPaginationInfo(response, content, _context);

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(100);
        result.NextCursor.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExtractPaginationInfoWithLinkHeaderExtractsNextUrl()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("Link", """<https://api.example.com/users?page=2>; rel="next", <https://api.example.com/users?page=10>; rel="last" """);
        var content = "[]";

        // Act
        var result = _protocol.ExtractPaginationInfo(response, content, _context);

        // Assert
        result.ShouldNotBeNull();
        result.NextCursor.ShouldBe("https://api.example.com/users?page=2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExtractPaginationInfoWithNoHeadersReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var content = "[]";

        // Act
        var result = _protocol.ExtractPaginationInfo(response, content, _context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExtractPaginationInfoWithBothHeadersReturnsBothValues()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-Total-Count", "500");
        response.Headers.Add("Link", """<https://api.example.com/items?cursor=abc123>; rel="next" """);
        var content = "[]";

        // Act
        var result = _protocol.ExtractPaginationInfo(response, content, _context);

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(500);
        result.NextCursor.ShouldBe("https://api.example.com/items?cursor=abc123");
    }

    #endregion

    #region BuildFullQueryString Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildFullQueryStringWithFilterPagingAndOrderingCombinesAll()
    {
        // Arrange
        var paging = new Mock<IPagingExpression>();
        paging.Setup(p => p.Skip).Returns(10);
        paging.Setup(p => p.Take).Returns(5);

        var direction = new Mock<ISortDirection>();
        direction.Setup(d => d.Name).Returns("Ascending");

        var field = new Mock<IOrderedField>();
        field.Setup(f => f.PropertyName).Returns("name");
        field.Setup(f => f.Direction).Returns(direction.Object);

        var ordering = new Mock<IOrderingExpression>();
        ordering.Setup(o => o.OrderedFields).Returns(new List<IOrderedField> { field.Object });

        var filterCondition = new FilterCondition
        {
            PropertyName = "status",
            Operator = FilterOperators.Equal,
            Value = "active"
        };

        var filter = new Mock<IFilterExpression>();
        filter.Setup(f => f.Root).Returns(filterCondition);

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Paging).Returns(paging.Object);
        command.Setup(c => c.Ordering).Returns(ordering.Object);
        command.Setup(c => c.Filter).Returns(filter.Object);

        // Act
        var result = _protocol.BuildFullQueryString(command.Object, _context);

        // Assert
        result.ShouldContain("status=active");
        result.ShouldContain("offset=10");
        result.ShouldContain("limit=5");
        result.ShouldContain("sort=name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildFullQueryStringWithNullFilterRootSkipsFilter()
    {
        // Arrange
        var paging = new Mock<IPagingExpression>();
        paging.Setup(p => p.Skip).Returns(0);
        paging.Setup(p => p.Take).Returns(10);

        var filter = new Mock<IFilterExpression>();
        filter.Setup(f => f.Root).Returns((IFilterNode?)null);

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Paging).Returns(paging.Object);
        command.Setup(c => c.Ordering).Returns((IOrderingExpression?)null);
        command.Setup(c => c.Filter).Returns(filter.Object);

        // Act
        var result = _protocol.BuildFullQueryString(command.Object, _context);

        // Assert
        result.ShouldBe("limit=10");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildFullQueryStringWithNoComponentsReturnsEmpty()
    {
        // Arrange
        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Paging).Returns((IPagingExpression?)null);
        command.Setup(c => c.Ordering).Returns((IOrderingExpression?)null);
        command.Setup(c => c.Filter).Returns((IFilterExpression?)null);

        // Act
        var result = _protocol.BuildFullQueryString(command.Object, _context);

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region ConfigureAuthenticationHeaders Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureAuthenticationHeadersWithApiKeyAddsHeader()
    {
        // Arrange
        var config = new HttpConnectionConfiguration
        {
            BaseUrl = "https://api.example.com"
        };
        var context = new HttpProtocolContext(
            Configuration: config,
            LoggerFactory: NullLoggerFactory.Instance,
            ResolvedCertificate: null,
            ResolvedPassword: null,
            ResolvedApiKey: "test-api-key-123");

        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");

        // Act
        _protocol.ConfigureAuthenticationHeaders(request, context);

        // Assert
        request.Headers.TryGetValues("X-API-Key", out var values).ShouldBeTrue();
        values!.First().ShouldBe("test-api-key-123");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureAuthenticationHeadersWithCustomHeaderNameUsesCustomName()
    {
        // Arrange
        var config = new HttpConnectionConfiguration
        {
            BaseUrl = "https://api.example.com",
            AuthenticationType = "ApiKey",
            AdditionalProperties = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["ApiKeyHeaderName"] = "Authorization-Key"
            }
        };
        var context = new HttpProtocolContext(
            Configuration: config,
            LoggerFactory: NullLoggerFactory.Instance,
            ResolvedCertificate: null,
            ResolvedPassword: null,
            ResolvedApiKey: "custom-key");

        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");

        // Act
        _protocol.ConfigureAuthenticationHeaders(request, context);

        // Assert
        request.Headers.TryGetValues("Authorization-Key", out var values).ShouldBeTrue();
        values!.First().ShouldBe("custom-key");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureAuthenticationHeadersWithNoApiKeyDoesNotAddHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");

        // Act
        _protocol.ConfigureAuthenticationHeaders(request, _context);

        // Assert
        request.Headers.TryGetValues("X-API-Key", out _).ShouldBeFalse();
    }

    #endregion

    #region ParseErrorResponse Edge Cases

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ParseErrorResponseWithNullErrorStringReturnsStatusCode()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Bad Request"
        };
        var content = """{"error": null}""";

        // Act
        var result = _protocol.ParseErrorResponse(response, content, _context);

        // Assert
        result.ShouldBe("HTTP 400: Bad Request");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ParseErrorResponseWithNullMessageReturnsStatusCodeOnly()
    {
        // Arrange - When "message" property exists but is null, returns code-only format
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Bad Request"
        };
        var content = """{"message": null}""";

        // Act
        var result = _protocol.ParseErrorResponse(response, content, _context);

        // Assert - Null check returns code-only format per implementation
        result.ShouldBe("HTTP 400");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ParseErrorResponseWithEmptyObjectReturnsStatusCode()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            ReasonPhrase = "Not Found"
        };
        var content = """{}""";

        // Act
        var result = _protocol.ParseErrorResponse(response, content, _context);

        // Assert
        result.ShouldBe("HTTP 404: Not Found");
    }

    #endregion

    #region ExtractPaginationInfo Edge Cases

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExtractPaginationInfoWithInvalidTotalCountReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-Total-Count", "not-a-number");
        var content = "[]";

        // Act
        var result = _protocol.ExtractPaginationInfo(response, content, _context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExtractPaginationInfoWithLinkHeaderRelNextNoQuotesExtractsUrl()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("Link", """<https://api.example.com/next>; rel=next""");
        var content = "[]";

        // Act
        var result = _protocol.ExtractPaginationInfo(response, content, _context);

        // Assert
        result.ShouldNotBeNull();
        result.NextCursor.ShouldBe("https://api.example.com/next");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExtractPaginationInfoWithLinkHeaderNoRelNextReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("Link", """<https://api.example.com/prev>; rel="prev" """);
        var content = "[]";

        // Act
        var result = _protocol.ExtractPaginationInfo(response, content, _context);

        // Assert
        result.ShouldBeNull();
    }

    #endregion
}
