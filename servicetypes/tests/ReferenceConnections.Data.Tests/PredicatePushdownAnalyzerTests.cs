using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Tests;

[Collection(nameof(DataServiceTestCollection))]
public class PredicatePushdownAnalyzerTests
{
    private readonly PredicatePushdownAnalyzer _analyzer;

    public PredicatePushdownAnalyzerTests()
    {
        _analyzer = new PredicatePushdownAnalyzer(
            NullLogger<PredicatePushdownAnalyzer>.Instance);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithNullLoggerThrows()
    {
        Should.Throw<ArgumentNullException>(() =>
            new PredicatePushdownAnalyzer(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TranslateToPhysicalWithNullFilterReturnsSuccessWithNullRoot()
    {
        // Act
        var result = _analyzer.TranslateToPhysical(null!, null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Root.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TranslateToPhysicalWithNullRootReturnsSuccessWithNullRoot()
    {
        // Arrange
        var filter = new FilterExpression { Root = null };

        // Act
        var result = _analyzer.TranslateToPhysical(filter, null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Root.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TranslateToPhysicalWithNoMappingsReturnsOriginalFieldNames()
    {
        // Arrange
        var mockOperator = new Mock<IFilterOperator>();
        var condition = new FilterCondition
        {
            PropertyName = "State",
            Operator = mockOperator.Object,
            Value = "Texas"
        };
        var filter = new FilterExpression { Root = condition };

        // Act
        var result = _analyzer.TranslateToPhysical(filter, null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var translated = result.Value!.Root.ShouldBeOfType<FilterCondition>();
        translated.PropertyName.ShouldBe("State");
        translated.Value.ShouldBe("Texas");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TranslateToPhysicalWithMappingsTranslatesFieldName()
    {
        // Arrange
        var mockOperator = new Mock<IFilterOperator>();
        var condition = new FilterCondition
        {
            PropertyName = "State",
            Operator = mockOperator.Object,
            Value = "Texas"
        };
        var filter = new FilterExpression { Root = condition };

        var fieldMappings = new Dictionary<string, string>
        {
            ["State"] = "StateCode"
        };

        // Act
        var result = _analyzer.TranslateToPhysical(filter, fieldMappings);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var translated = result.Value!.Root.ShouldBeOfType<FilterCondition>();
        translated.PropertyName.ShouldBe("StateCode");
        translated.Value.ShouldBe("Texas");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TranslateToPhysicalPreservesOperator()
    {
        // Arrange
        var mockOperator = new Mock<IFilterOperator>();
        var condition = new FilterCondition
        {
            PropertyName = "Name",
            Operator = mockOperator.Object,
            Value = "Acme"
        };
        var filter = new FilterExpression { Root = condition };

        var fieldMappings = new Dictionary<string, string>
        {
            ["Name"] = "FullName"
        };

        // Act
        var result = _analyzer.TranslateToPhysical(filter, fieldMappings);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var translated = result.Value!.Root.ShouldBeOfType<FilterCondition>();
        translated.Operator.ShouldBe(mockOperator.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TranslateToPhysicalWithUnmappedFieldUsesOriginalName()
    {
        // Arrange
        var mockOperator = new Mock<IFilterOperator>();
        var condition = new FilterCondition
        {
            PropertyName = "UnmappedField",
            Operator = mockOperator.Object,
            Value = 42
        };
        var filter = new FilterExpression { Root = condition };

        var fieldMappings = new Dictionary<string, string>
        {
            ["State"] = "StateCode"
        };

        // Act
        var result = _analyzer.TranslateToPhysical(filter, fieldMappings);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var translated = result.Value!.Root.ShouldBeOfType<FilterCondition>();
        translated.PropertyName.ShouldBe("UnmappedField");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TranslateToPhysicalTranslatesFilterGroupRecursively()
    {
        // Arrange
        var mockOperator = new Mock<IFilterOperator>();
        var condition1 = new FilterCondition
        {
            PropertyName = "State",
            Operator = mockOperator.Object,
            Value = "Texas"
        };
        var condition2 = new FilterCondition
        {
            PropertyName = "City",
            Operator = mockOperator.Object,
            Value = "Dallas"
        };
        var group = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = new List<IFilterNode> { condition1, condition2 }
        };
        var filter = new FilterExpression { Root = group };

        var fieldMappings = new Dictionary<string, string>
        {
            ["State"] = "state_code",
            ["City"] = "city_name"
        };

        // Act
        var result = _analyzer.TranslateToPhysical(filter, fieldMappings);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var translatedGroup = result.Value!.Root.ShouldBeOfType<FilterGroup>();
        translatedGroup.Operator.ShouldBe(LogicalOperator.And);
        translatedGroup.Nodes.Count.ShouldBe(2);

        var translatedCond1 = translatedGroup.Nodes[0].ShouldBeOfType<FilterCondition>();
        translatedCond1.PropertyName.ShouldBe("state_code");

        var translatedCond2 = translatedGroup.Nodes[1].ShouldBeOfType<FilterCondition>();
        translatedCond2.PropertyName.ShouldBe("city_name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TranslateToPhysicalTranslatesNestedFilterGroups()
    {
        // Arrange
        var mockOperator = new Mock<IFilterOperator>();
        var innerCondition = new FilterCondition
        {
            PropertyName = "Country",
            Operator = mockOperator.Object,
            Value = "US"
        };
        var innerGroup = new FilterGroup
        {
            Operator = LogicalOperator.Or,
            Nodes = new List<IFilterNode> { innerCondition }
        };
        var outerCondition = new FilterCondition
        {
            PropertyName = "Status",
            Operator = mockOperator.Object,
            Value = "Active"
        };
        var outerGroup = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = new List<IFilterNode> { innerGroup, outerCondition }
        };
        var filter = new FilterExpression { Root = outerGroup };

        var fieldMappings = new Dictionary<string, string>
        {
            ["Country"] = "country_code",
            ["Status"] = "status_flag"
        };

        // Act
        var result = _analyzer.TranslateToPhysical(filter, fieldMappings);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var translatedOuter = result.Value!.Root.ShouldBeOfType<FilterGroup>();
        translatedOuter.Operator.ShouldBe(LogicalOperator.And);
        translatedOuter.Nodes.Count.ShouldBe(2);

        var translatedInner = translatedOuter.Nodes[0].ShouldBeOfType<FilterGroup>();
        translatedInner.Operator.ShouldBe(LogicalOperator.Or);

        var nestedCondition = translatedInner.Nodes[0].ShouldBeOfType<FilterCondition>();
        nestedCondition.PropertyName.ShouldBe("country_code");

        var translatedStatus = translatedOuter.Nodes[1].ShouldBeOfType<FilterCondition>();
        translatedStatus.PropertyName.ShouldBe("status_flag");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TranslateToPhysicalWithEmptyMappingsUsesOriginalNames()
    {
        // Arrange
        var mockOperator = new Mock<IFilterOperator>();
        var condition = new FilterCondition
        {
            PropertyName = "Name",
            Operator = mockOperator.Object,
            Value = "Test"
        };
        var filter = new FilterExpression { Root = condition };

        var emptyMappings = new Dictionary<string, string>();

        // Act
        var result = _analyzer.TranslateToPhysical(filter, emptyMappings);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var translated = result.Value!.Root.ShouldBeOfType<FilterCondition>();
        translated.PropertyName.ShouldBe("Name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourceWithNullFilterReturnsEmptyDictionary()
    {
        // Arrange
        var dataset = new DataSetConfiguration();
        var sources = new List<DataSetSourceConfiguration>();
        var fieldMappings = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        // Act
        var result = _analyzer.DecomposeBySource(null, dataset, sources, fieldMappings);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourceWithNullRootReturnsEmptyDictionary()
    {
        // Arrange
        var filter = new FilterExpression { Root = null };
        var dataset = new DataSetConfiguration();
        var sources = new List<DataSetSourceConfiguration>();
        var fieldMappings = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        // Act
        var result = _analyzer.DecomposeBySource(filter, dataset, sources, fieldMappings);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourceWithNullDataSetReturnsFailure()
    {
        // Arrange
        var mockOperator = new Mock<IFilterOperator>();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "Name",
                Operator = mockOperator.Object,
                Value = "Test"
            }
        };
        var sources = new List<DataSetSourceConfiguration>();
        var fieldMappings = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        // Act
        var result = _analyzer.DecomposeBySource(filter, null!, sources, fieldMappings);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourceWithNullSourcesReturnsFailure()
    {
        // Arrange
        var mockOperator = new Mock<IFilterOperator>();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "Name",
                Operator = mockOperator.Object,
                Value = "Test"
            }
        };
        var dataset = new DataSetConfiguration();
        var fieldMappings = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        // Act
        var result = _analyzer.DecomposeBySource(filter, dataset, null!, fieldMappings);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }
}
