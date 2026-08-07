using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Http;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Protocols;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReferenceConnections.Http.Tests.Protocols;

/// <summary>
/// Tests for GraphQLProtocolBase virtual methods and helper functionality.
/// </summary>
public class GraphQLProtocolBaseTests
{
    private readonly TestableGraphQLProtocol _protocol;
    private readonly HttpProtocolContext _context;

    public GraphQLProtocolBaseTests()
    {
        _protocol = new TestableGraphQLProtocol();
        _context = CreateTestContext();
    }

    private static HttpProtocolContext CreateTestContext()
    {
        // Why: Name is a header field on ConnectionConfiguration after config-split; cannot be set on typed body.
        var config = new HttpConnectionConfiguration
        {
            BaseUrl = "https://api.example.com/graphql"
        };
        return new HttpProtocolContext(
            Configuration: config,
            LoggerFactory: NullLoggerFactory.Instance,
            ResolvedCertificate: null,
            ResolvedPassword: null,
            ResolvedApiKey: null);
    }

    #region GetGraphQLTypeName Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetGraphQLTypeNameWithPascalCaseReturnsLowerCamelCase()
    {
        // Act
        var result = _protocol.GetGraphQLTypeName("Users");

        // Assert
        result.ShouldBe("users");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetGraphQLTypeNameWithEmptyStringReturnsData()
    {
        // Act
        var result = _protocol.GetGraphQLTypeName(string.Empty);

        // Assert
        result.ShouldBe("data");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetGraphQLTypeNameWithNullReturnsData()
    {
        // Act
        var result = _protocol.GetGraphQLTypeName(null!);

        // Assert
        result.ShouldBe("data");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetGraphQLTypeNameWithMultiWordKeepsRest()
    {
        // Act
        var result = _protocol.GetGraphQLTypeName("UserAccounts");

        // Assert
        result.ShouldBe("userAccounts");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetGraphQLTypeNameWithLowerCaseReturnsUnchanged()
    {
        // Act
        var result = _protocol.GetGraphQLTypeName("users");

        // Assert
        result.ShouldBe("users");
    }

    #endregion

    #region BuildFieldSelection Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildFieldSelectionWithNoContainerReturnsId()
    {
        // Act
        var result = _protocol.BuildFieldSelection(null!);

        // Assert
        result.ShouldBe("id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildFieldSelectionWithNoSchemaReturnsId()
    {
        // Arrange
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Schema).Returns((IContainerSchema?)null!);

        // Act
        var result = _protocol.BuildFieldSelection(container.Object);

        // Assert
        result.ShouldBe("id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildFieldSelectionWithEmptyFieldsReturnsId()
    {
        // Arrange
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(new List<IField>());

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Schema).Returns(schema.Object);

        // Act
        var result = _protocol.BuildFieldSelection(container.Object);

        // Assert
        result.ShouldBe("id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildFieldSelectionWithFieldsReturnsSpaceSeparatedNames()
    {
        // Arrange
        var field1 = new Mock<IField>();
        field1.Setup(f => f.Name).Returns("id");

        var field2 = new Mock<IField>();
        field2.Setup(f => f.Name).Returns("name");

        var field3 = new Mock<IField>();
        field3.Setup(f => f.Name).Returns("email");

        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(new List<IField> { field1.Object, field2.Object, field3.Object });

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Schema).Returns(schema.Object);

        // Act
        var result = _protocol.BuildFieldSelection(container.Object);

        // Assert
        result.ShouldBe("id name email");
    }

    #endregion

    #region MapOperatorToGraphQL Tests

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    [InlineData("Equal", "eq")]
    [InlineData("NotEqual", "ne")]
    [InlineData("GreaterThan", "gt")]
    [InlineData("GreaterThanOrEqual", "gte")]
    [InlineData("LessThan", "lt")]
    [InlineData("LessThanOrEqual", "lte")]
    [InlineData("Contains", "contains")]
    [InlineData("StartsWith", "startsWith")]
    [InlineData("EndsWith", "endsWith")]
    [InlineData("In", "in")]
    [InlineData("NotIn", "notIn")]
    [InlineData("Unknown", "eq")]
    public void MapOperatorToGraphQLReturnsExpectedOperator(string input, string expected)
    {
        // Act
        var result = _protocol.MapOperatorToGraphQL(input);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region BuildQueryArguments Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildQueryArgumentsWithNoDataReturnsEmpty()
    {
        // Arrange
        var command = new Mock<IDataCommand>();

        // Act
        var result = _protocol.BuildQueryArguments(command.Object);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildQueryArgumentsWithPagingReturnsSkipAndTake()
    {
        // Arrange
        var paging = new Mock<IPagingExpression>();
        paging.Setup(p => p.Skip).Returns(10);
        paging.Setup(p => p.Take).Returns(25);

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Paging).Returns(paging.Object);
        command.Setup(c => c.Filter).Returns((IFilterExpression?)null);
        command.Setup(c => c.Ordering).Returns((IOrderingExpression?)null);

        // Act
        var result = _protocol.BuildQueryArguments(command.Object);

        // Assert
        result.ShouldContain("skip: 10");
        result.ShouldContain("take: 25");
        result.ShouldStartWith("(");
        result.ShouldEndWith(")");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildQueryArgumentsWithFilterAddsFilterVariable()
    {
        // Arrange
        var filter = new Mock<IFilterExpression>();

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Paging).Returns((IPagingExpression?)null);
        command.Setup(c => c.Filter).Returns(filter.Object);
        command.Setup(c => c.Ordering).Returns((IOrderingExpression?)null);

        // Act
        var result = _protocol.BuildQueryArguments(command.Object);

        // Assert
        result.ShouldBe("(filter: $filter)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildQueryArgumentsWithOrderingAddsOrderByVariable()
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
        command.Setup(c => c.Paging).Returns((IPagingExpression?)null);
        command.Setup(c => c.Filter).Returns((IFilterExpression?)null);
        command.Setup(c => c.Ordering).Returns(ordering.Object);

        // Act
        var result = _protocol.BuildQueryArguments(command.Object);

        // Assert
        result.ShouldBe("(orderBy: $orderBy)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildQueryArgumentsWithAllArgumentsCombinesThem()
    {
        // Arrange
        var paging = new Mock<IPagingExpression>();
        paging.Setup(p => p.Skip).Returns(5);
        paging.Setup(p => p.Take).Returns(10);

        var filter = new Mock<IFilterExpression>();

        var direction = new Mock<ISortDirection>();
        direction.Setup(d => d.Name).Returns("Descending");

        var field = new Mock<IOrderedField>();
        field.Setup(f => f.PropertyName).Returns("createdAt");
        field.Setup(f => f.Direction).Returns(direction.Object);

        var ordering = new Mock<IOrderingExpression>();
        ordering.Setup(o => o.OrderedFields).Returns(new List<IOrderedField> { field.Object });

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Paging).Returns(paging.Object);
        command.Setup(c => c.Filter).Returns(filter.Object);
        command.Setup(c => c.Ordering).Returns(ordering.Object);

        // Act
        var result = _protocol.BuildQueryArguments(command.Object);

        // Assert
        result.ShouldContain("skip: 5");
        result.ShouldContain("take: 10");
        result.ShouldContain("filter: $filter");
        result.ShouldContain("orderBy: $orderBy");
    }

    #endregion

    #region BuildQueryOperation Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildQueryOperationBuildsCorrectQueryStructure()
    {
        // Arrange
        var command = new Mock<IDataCommand>();

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Schema).Returns((IContainerSchema?)null!);
        container.Setup(c => c.Name).Returns("Users");

        // Act
        var result = _protocol.BuildQueryOperation(command.Object, container.Object);

        // Assert
        result.ShouldBe("query { users { id } }");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildQueryOperationWithFieldsIncludesFieldSelection()
    {
        // Arrange
        var command = new Mock<IDataCommand>();

        var field1 = new Mock<IField>();
        field1.Setup(f => f.Name).Returns("id");

        var field2 = new Mock<IField>();
        field2.Setup(f => f.Name).Returns("title");

        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(new List<IField> { field1.Object, field2.Object });

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.Name).Returns("Products");

        // Act
        var result = _protocol.BuildQueryOperation(command.Object, container.Object);

        // Assert
        result.ShouldBe("query { products { id title } }");
    }

    #endregion

    #region BuildMutationOperation Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildMutationOperationWithCreateBuildsCorrectStructure()
    {
        // Arrange
        var command = new Mock<IDataCommand>();

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Schema).Returns((IContainerSchema?)null!);
        container.Setup(c => c.Name).Returns("User");

        // Act
        var result = _protocol.BuildMutationOperation(command.Object, container.Object, "create");

        // Assert - type name is converted to camelCase: User -> user
        result.ShouldBe("mutation { createuser(input: $input) { id } }");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildMutationOperationWithUpdateBuildsCorrectStructure()
    {
        // Arrange
        var command = new Mock<IDataCommand>();

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Schema).Returns((IContainerSchema?)null!);
        container.Setup(c => c.Name).Returns("Product");

        // Act
        var result = _protocol.BuildMutationOperation(command.Object, container.Object, "update");

        // Assert - type name is converted to camelCase: Product -> product
        result.ShouldBe("mutation { updateproduct(input: $input) { id } }");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildMutationOperationWithDeleteBuildsCorrectStructure()
    {
        // Arrange
        var command = new Mock<IDataCommand>();

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Schema).Returns((IContainerSchema?)null!);
        container.Setup(c => c.Name).Returns("Order");

        // Act
        var result = _protocol.BuildMutationOperation(command.Object, container.Object, "delete");

        // Assert - type name is converted to camelCase: Order -> order
        result.ShouldBe("mutation { deleteorder(input: $input) { id } }");
    }

    #endregion

    #region GetOperationName Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetOperationNameWithMetadataReturnsMetadataValue()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { ["GraphQLOperationName"] = "CustomOperation" };

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns(metadata);

        var container = new Mock<IStorageContainer>();

        // Act
        var result = _protocol.GetOperationName(command.Object, container.Object, _context);

        // Assert
        result.ShouldBe("CustomOperation");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetOperationNameWithQueryCommandReturnsGetPrefix()
    {
        // Arrange
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        command.Setup(c => c.CommandType).Returns("Query");

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("Users");

        // Act
        var result = _protocol.GetOperationName(command.Object, container.Object, _context);

        // Assert
        result.ShouldBe("GetUsers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetOperationNameWithInsertCommandReturnsCreatePrefix()
    {
        // Arrange
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        command.Setup(c => c.CommandType).Returns("Insert");

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("User");

        // Act
        var result = _protocol.GetOperationName(command.Object, container.Object, _context);

        // Assert
        result.ShouldBe("CreateUser");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetOperationNameWithUpdateCommandReturnsUpdatePrefix()
    {
        // Arrange
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        command.Setup(c => c.CommandType).Returns("Update");

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("Product");

        // Act
        var result = _protocol.GetOperationName(command.Object, container.Object, _context);

        // Assert
        result.ShouldBe("UpdateProduct");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetOperationNameWithDeleteCommandReturnsDeletePrefix()
    {
        // Arrange
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        command.Setup(c => c.CommandType).Returns("Delete");

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("Order");

        // Act
        var result = _protocol.GetOperationName(command.Object, container.Object, _context);

        // Assert
        result.ShouldBe("DeleteOrder");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetOperationNameWithUnknownCommandTypeReturnsNull()
    {
        // Arrange
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        command.Setup(c => c.CommandType).Returns("CustomCommand");

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("Data");

        // Act
        var result = _protocol.GetOperationName(command.Object, container.Object, _context);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region FormatGraphQLError Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FormatGraphQLErrorWithMessageOnlyReturnsMessage()
    {
        // Arrange
        var error = new GraphQLError { Message = "Field 'name' not found" };

        // Act
        var result = _protocol.FormatGraphQLError(error);

        // Assert
        result.ShouldBe("Field 'name' not found");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FormatGraphQLErrorWithNullMessageReturnsUnknownError()
    {
        // Arrange
        var error = new GraphQLError { Message = null };

        // Act
        var result = _protocol.FormatGraphQLError(error);

        // Assert
        result.ShouldBe("Unknown error");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FormatGraphQLErrorWithLocationIncludesLineAndColumn()
    {
        // Arrange
        var error = new GraphQLError
        {
            Message = "Syntax error",
            Locations =
            [
                new GraphQLErrorLocation { Line = 3, Column = 15 }
            ]
        };

        // Act
        var result = _protocol.FormatGraphQLError(error);

        // Assert
        result.ShouldBe("Syntax error at line 3, column 15");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FormatGraphQLErrorWithPathIncludesPath()
    {
        // Arrange
        var error = new GraphQLError
        {
            Message = "Not authorized",
            Path = ["user", "email"]
        };

        // Act
        var result = _protocol.FormatGraphQLError(error);

        // Assert
        result.ShouldBe("Not authorized (path: user.email)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FormatGraphQLErrorWithAllInfoIncludesEverything()
    {
        // Arrange
        var error = new GraphQLError
        {
            Message = "Validation error",
            Locations =
            [
                new GraphQLErrorLocation { Line = 5, Column = 10 }
            ],
            Path = ["mutation", "createUser", "input"]
        };

        // Act
        var result = _protocol.FormatGraphQLError(error);

        // Assert
        result.ShouldBe("Validation error at line 5, column 10 (path: mutation.createUser.input)");
    }

    #endregion

    #region BuildFilterObjectFromExpression Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildFilterObjectFromExpressionWithConditionBuildsFilterObject()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "status",
            Operator = FilterOperators.Equal,
            Value = "active"
        };

        // Act
        var result = _protocol.BuildFilterObjectFromExpression(condition);

        // Assert
        result.ShouldNotBeNull();
        var dict = result as Dictionary<string, object?>;
        dict.ShouldNotBeNull();
        dict.ShouldContainKey("status");
        var statusFilter = dict["status"] as Dictionary<string, object?>;
        statusFilter.ShouldNotBeNull();
        statusFilter.ShouldContainKey("eq");
        statusFilter["eq"].ShouldBe("active");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildFilterObjectFromExpressionWithGroupBuildsLogicalFilter()
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
                    PropertyName = "role",
                    Operator = FilterOperators.Equal,
                    Value = "admin"
                }
            ]
        };

        // Act
        var result = _protocol.BuildFilterObjectFromExpression(group);

        // Assert
        result.ShouldNotBeNull();
        var dict = result as Dictionary<string, object?>;
        dict.ShouldNotBeNull();
        dict.ShouldContainKey("AND");
        var conditions = dict["AND"] as List<object?>;
        conditions.ShouldNotBeNull();
        conditions.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildFilterObjectFromExpressionWithNullReturnsNull()
    {
        // Arrange - Create a mock IFilterNode that is neither condition nor group
        var unknownNode = new Mock<IFilterNode>();

        // Act
        var result = _protocol.BuildFilterObjectFromExpression(unknownNode.Object);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region BuildFilterVariable Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildFilterVariableWithNullRootReturnsNull()
    {
        // Arrange
        var filter = new Mock<IFilterExpression>();
        filter.Setup(f => f.Root).Returns((IFilterNode?)null);

        // Act
        var result = _protocol.BuildFilterVariable(filter.Object);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildFilterVariableWithRootBuildsFilter()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "active",
            Operator = FilterOperators.Equal,
            Value = true
        };

        var filter = new Mock<IFilterExpression>();
        filter.Setup(f => f.Root).Returns(condition);

        // Act
        var result = _protocol.BuildFilterVariable(filter.Object);

        // Assert
        result.ShouldNotBeNull();
        var dict = result as Dictionary<string, object?>;
        dict.ShouldNotBeNull();
        dict.ShouldContainKey("active");
    }

    #endregion

    #region BuildQueryArguments Edge Cases

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildQueryArgumentsWithSkipZeroDoesNotIncludeSkip()
    {
        // Arrange
        var paging = new Mock<IPagingExpression>();
        paging.Setup(p => p.Skip).Returns(0);
        paging.Setup(p => p.Take).Returns(25);

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Paging).Returns(paging.Object);
        command.Setup(c => c.Filter).Returns((IFilterExpression?)null);
        command.Setup(c => c.Ordering).Returns((IOrderingExpression?)null);

        // Act
        var result = _protocol.BuildQueryArguments(command.Object);

        // Assert
        result.ShouldNotContain("skip");
        result.ShouldContain("take: 25");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildQueryArgumentsWithTakeZeroDoesNotIncludeTake()
    {
        // Arrange
        var paging = new Mock<IPagingExpression>();
        paging.Setup(p => p.Skip).Returns(10);
        paging.Setup(p => p.Take).Returns(0);

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Paging).Returns(paging.Object);
        command.Setup(c => c.Filter).Returns((IFilterExpression?)null);
        command.Setup(c => c.Ordering).Returns((IOrderingExpression?)null);

        // Act
        var result = _protocol.BuildQueryArguments(command.Object);

        // Assert
        result.ShouldContain("skip: 10");
        result.ShouldNotContain("take");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void BuildQueryArgumentsWithEmptyOrderedFieldsDoesNotIncludeOrderBy()
    {
        // Arrange
        var ordering = new Mock<IOrderingExpression>();
        ordering.Setup(o => o.OrderedFields).Returns(new List<IOrderedField>());

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Paging).Returns((IPagingExpression?)null);
        command.Setup(c => c.Filter).Returns((IFilterExpression?)null);
        command.Setup(c => c.Ordering).Returns(ordering.Object);

        // Act
        var result = _protocol.BuildQueryArguments(command.Object);

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region BuildGraphQLQuery Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task BuildGraphQLQueryWithExplicitQueryReturnsMetadataQuery()
    {
        // Arrange
        var explicitQuery = "query GetUser($id: ID!) { user(id: $id) { id name email } }";
        var metadata = new Dictionary<string, object> { ["GraphQLQuery"] = explicitQuery };

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns(metadata);

        var container = new Mock<IStorageContainer>();

        // Act
        var result = await _protocol.BuildGraphQLQuery(command.Object, container.Object, _context, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(explicitQuery);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task BuildGraphQLQueryWithSelectCommandBuildsQuery()
    {
        // Arrange
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        command.Setup(c => c.CommandType).Returns("Select");

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Schema).Returns((IContainerSchema?)null!);
        container.Setup(c => c.Name).Returns("Users");

        // Act
        var result = await _protocol.BuildGraphQLQuery(command.Object, container.Object, _context, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("query");
        result.Value!.ShouldContain("users");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task BuildGraphQLQueryWithCreateCommandBuildsMutation()
    {
        // Arrange
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        command.Setup(c => c.CommandType).Returns("Create");

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Schema).Returns((IContainerSchema?)null!);
        container.Setup(c => c.Name).Returns("User");

        // Act
        var result = await _protocol.BuildGraphQLQuery(command.Object, container.Object, _context, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("mutation");
        result.Value!.ShouldContain("createuser");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task BuildGraphQLQueryWithUnknownCommandTypeBuildsQuery()
    {
        // Arrange
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        command.Setup(c => c.CommandType).Returns("Custom");

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Schema).Returns((IContainerSchema?)null!);
        container.Setup(c => c.Name).Returns("Data");

        // Act
        var result = await _protocol.BuildGraphQLQuery(command.Object, container.Object, _context, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("query");
    }

    #endregion

    #region BuildVariables Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task BuildVariablesWithNoDataReturnsNull()
    {
        // Arrange
        var command = new Mock<IDataCommand>();

        var container = new Mock<IStorageContainer>();

        // Act
        var result = await _protocol.BuildVariables(command.Object, container.Object, _context, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task BuildVariablesWithInputDataIncludesInput()
    {
        // Arrange
        var inputData = new { Name = "Test", Value = 42 };

        var command = new Mock<IDataCommandWithInput>();
        command.Setup(c => c.InputData).Returns(inputData);

        var container = new Mock<IStorageContainer>();

        // Act
        var result = await _protocol.BuildVariables(command.Object, container.Object, _context, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContainKey("input");
        result["input"].ShouldBe(inputData);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task BuildVariablesWithFilterIncludesFilterVariable()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "active",
            Operator = FilterOperators.Equal,
            Value = true
        };

        var filter = new Mock<IFilterExpression>();
        filter.Setup(f => f.Root).Returns(condition);

        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.Filter).Returns(filter.Object);
        command.Setup(c => c.Ordering).Returns((IOrderingExpression?)null);

        var container = new Mock<IStorageContainer>();

        // Act
        var result = await _protocol.BuildVariables(command.Object, container.Object, _context, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContainKey("filter");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task BuildVariablesWithOrderingIncludesOrderByVariable()
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
        command.Setup(c => c.Filter).Returns((IFilterExpression?)null);
        command.Setup(c => c.Ordering).Returns(ordering.Object);

        var container = new Mock<IStorageContainer>();

        // Act
        var result = await _protocol.BuildVariables(command.Object, container.Object, _context, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContainKey("orderBy");
        var orderBy = result["orderBy"] as List<IDictionary<string, string>>;
        orderBy.ShouldNotBeNull();
        orderBy.Count.ShouldBe(1);
        orderBy[0]["field"].ShouldBe("name");
        orderBy[0]["direction"].ShouldBe("ASCENDING");
    }

    #endregion

    #region ConfigureGraphQLHeaders Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureGraphQLHeadersAddsAcceptHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/graphql");

        // Act
        _protocol.ConfigureGraphQLHeaders(request, _context);

        // Assert
        request.Headers.Accept.ShouldContain(h => h.MediaType == "application/json");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureGraphQLHeadersWithApiKeyAddsApiKeyHeader()
    {
        // Arrange
        var config = new HttpConnectionConfiguration
        {
            BaseUrl = "https://api.example.com/graphql"
        };
        var context = new HttpProtocolContext(
            Configuration: config,
            LoggerFactory: NullLoggerFactory.Instance,
            ResolvedCertificate: null,
            ResolvedPassword: null,
            ResolvedApiKey: "test-api-key");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/graphql");

        // Act
        _protocol.ConfigureGraphQLHeaders(request, context);

        // Assert
        request.Headers.TryGetValues("X-API-Key", out var values).ShouldBeTrue();
        values.ShouldContain("test-api-key");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureGraphQLHeadersWithCustomApiKeyHeaderNameUsesCustomName()
    {
        // Arrange
        var config = new HttpConnectionConfiguration
        {
            BaseUrl = "https://api.example.com/graphql",
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

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/graphql");

        // Act
        _protocol.ConfigureGraphQLHeaders(request, context);

        // Assert
        request.Headers.TryGetValues("Authorization-Key", out var values).ShouldBeTrue();
        values.ShouldContain("custom-key");
    }

    #endregion

    #region FormatGraphQLError Edge Cases

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FormatGraphQLErrorWithEmptyLocationsDoesNotIncludeLocation()
    {
        // Arrange
        var error = new GraphQLError
        {
            Message = "Test error",
            Locations = []
        };

        // Act
        var result = _protocol.FormatGraphQLError(error);

        // Assert
        result.ShouldBe("Test error");
        result.ShouldNotContain("line");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FormatGraphQLErrorWithEmptyPathDoesNotIncludePath()
    {
        // Arrange
        var error = new GraphQLError
        {
            Message = "Test error",
            Path = []
        };

        // Act
        var result = _protocol.FormatGraphQLError(error);

        // Assert
        result.ShouldBe("Test error");
        result.ShouldNotContain("path");
    }

    #endregion

    #region GetOperationName Edge Cases

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetOperationNameWithNullContainerNameReturnsGetWithEmptySuffix()
    {
        // Why: Container.Name comes from IStorageContainer, not IDataCommand (addressing moved
        // to DataStoreTarget). When container.Name is null, string interpolation produces "Get".
        // Arrange
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        command.Setup(c => c.CommandType).Returns("Query");

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns((string?)null!);

        // Act
        var result = _protocol.GetOperationName(command.Object, container.Object, _context);

        // Assert
        result.ShouldBe("Get");
    }

    #endregion
}

/// <summary>
/// Testable GraphQL protocol that exposes protected methods.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class TestableGraphQLProtocol : GraphQLProtocolBase
{
    public TestableGraphQLProtocol()
        : base(997, "TestGraphQL", "Test GraphQL protocol for unit tests")
    {
    }

    public new string GetGraphQLTypeName(string containerName)
        => base.GetGraphQLTypeName(containerName);

    public new string BuildFieldSelection(IStorageContainer container)
        => base.BuildFieldSelection(container);

    public new string MapOperatorToGraphQL(string operatorName)
        => base.MapOperatorToGraphQL(operatorName);

    public new string BuildQueryArguments(IDataCommand command)
        => base.BuildQueryArguments(command);

    public new string BuildQueryOperation(IDataCommand command, IStorageContainer container)
        => base.BuildQueryOperation(command, container);

    public new string BuildMutationOperation(IDataCommand command, IStorageContainer container, string action)
        => base.BuildMutationOperation(command, container, action);

    public new string? GetOperationName(IDataCommand command, IStorageContainer container, HttpProtocolContext context)
        => base.GetOperationName(command, container, context);

    public new string FormatGraphQLError(GraphQLError error)
        => base.FormatGraphQLError(error);

    public new object? BuildFilterObjectFromExpression(IFilterNode node)
        => base.BuildFilterObjectFromExpression(node);

    public new object? BuildFilterVariable(IFilterExpression filter)
        => base.BuildFilterVariable(filter);

    public new Task<IGenericResult<string>> BuildGraphQLQuery(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
        => base.BuildGraphQLQuery(command, container, context, cancellationToken);

    public new Task<IDictionary<string, object?>?> BuildVariables(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
        => base.BuildVariables(command, container, context, cancellationToken);

    public new void ConfigureGraphQLHeaders(HttpRequestMessage request, HttpProtocolContext context)
        => base.ConfigureGraphQLHeaders(request, context);
}
