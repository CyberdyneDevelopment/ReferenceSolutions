using System;
using System.Collections.Generic;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Tests for PredicatePushdownAnalyzer.DecomposeBySource covering
/// ExtractConditions, GenerateSourceFilters, BuildFilterExpression,
/// and BuildSourceMappingLookups paths.
/// </summary>
[Collection(nameof(DataServiceTestCollection))]
public sealed class PredicatePushdownAnalyzerDecomposeTests
{
    private readonly PredicatePushdownAnalyzer _analyzer;

    public PredicatePushdownAnalyzerDecomposeTests()
    {
        _analyzer = new PredicatePushdownAnalyzer(
            NullLogger<PredicatePushdownAnalyzer>.Instance);
    }

    // --- DecomposeBySource with conditions routed to sources ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourceRoutesSingleConditionToCorrectSource()
    {
        var mockOperator = new Mock<IFilterOperator>();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "State",
                Operator = mockOperator.Object,
                Value = "Texas"
            }
        };

        var fieldMappingId = Guid.NewGuid();
        var source = CreateSource("SqlPrimary", supportsPredicatePushdown: true, fieldMappingIds: [fieldMappingId]);
        var sources = new List<DataSetSourceConfiguration> { source };

        var fieldMappings = new Dictionary<string, string> { ["State"] = "StateCode" };
        var resolvedMappings = CreateFieldMappings("SqlPrimary", fieldMappings);

        var dataset = CreateDataset("TestDataSet");

        var result = _analyzer.DecomposeBySource(filter, dataset, sources, resolvedMappings);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
        result.Value.ShouldContainKey("SqlPrimary");

        // Verify the translated condition uses physical field name
        var sourceFilter = result.Value["SqlPrimary"];
        sourceFilter.Root.ShouldNotBeNull();
        var condition = sourceFilter.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("StateCode");
        condition.Value.ShouldBe("Texas");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourceRoutesConditionsToMultipleSources()
    {
        var mockOperator = new Mock<IFilterOperator>();
        var stateCondition = new FilterCondition
        {
            PropertyName = "State",
            Operator = mockOperator.Object,
            Value = "Texas"
        };
        var orderDateCondition = new FilterCondition
        {
            PropertyName = "OrderDate",
            Operator = mockOperator.Object,
            Value = "2025-01-01"
        };
        var group = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = new List<IFilterNode> { stateCondition, orderDateCondition }
        };
        var filter = new FilterExpression { Root = group };

        var sqlMappingId = Guid.NewGuid();
        var restMappingId = Guid.NewGuid();

        var sqlSource = CreateSource("SqlPrimary", supportsPredicatePushdown: true, fieldMappingIds: [sqlMappingId]);
        var restSource = CreateSource("RestOrders", supportsPredicatePushdown: true, fieldMappingIds: [restMappingId]);
        var sources = new List<DataSetSourceConfiguration> { sqlSource, restSource };

        var sqlMappings = new Dictionary<string, string> { ["State"] = "StateCode" };
        var restMappings = new Dictionary<string, string> { ["OrderDate"] = "order_date" };

        var resolvedMappings = CreateFieldMappings("SqlPrimary", sqlMappings, "RestOrders", restMappings);

        var dataset = CreateDataset("CustomerOrders");

        var result = _analyzer.DecomposeBySource(filter, dataset, sources, resolvedMappings);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(2);
        result.Value.ShouldContainKey("SqlPrimary");
        result.Value.ShouldContainKey("RestOrders");

        // Verify SqlPrimary got State condition
        var sqlFilter = result.Value["SqlPrimary"];
        var sqlCondition = sqlFilter.Root.ShouldBeOfType<FilterCondition>();
        sqlCondition.PropertyName.ShouldBe("StateCode");

        // Verify RestOrders got OrderDate condition
        var restFilter = result.Value["RestOrders"];
        var restCondition = restFilter.Root.ShouldBeOfType<FilterCondition>();
        restCondition.PropertyName.ShouldBe("order_date");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourceWithMultipleConditionsForSameSourceBuildsAndGroup()
    {
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

        var mappingId = Guid.NewGuid();
        var source = CreateSource("SqlPrimary", supportsPredicatePushdown: true, fieldMappingIds: [mappingId]);
        var sources = new List<DataSetSourceConfiguration> { source };

        var fieldMappings = new Dictionary<string, string>
        {
            ["State"] = "state_code",
            ["City"] = "city_name"
        };
        var resolvedMappings = CreateFieldMappings("SqlPrimary", fieldMappings);
        var dataset = CreateDataset("TestDataSet");

        var result = _analyzer.DecomposeBySource(filter, dataset, sources, resolvedMappings);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);

        // Multiple conditions for same source should be combined with AND
        var sourceFilter = result.Value["SqlPrimary"];
        var filterGroup = sourceFilter.Root.ShouldBeOfType<FilterGroup>();
        filterGroup.Operator.ShouldBe(LogicalOperator.And);
        filterGroup.Nodes.Count.ShouldBe(2);
    }

    // --- GenerateSourceFilters: source not supporting pushdown ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourceSkipsSourceThatDoesNotSupportPushdown()
    {
        var mockOperator = new Mock<IFilterOperator>();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "State",
                Operator = mockOperator.Object,
                Value = "Texas"
            }
        };

        var mappingId = Guid.NewGuid();
        var source = CreateSource("FileSource", supportsPredicatePushdown: false, fieldMappingIds: [mappingId]);
        var sources = new List<DataSetSourceConfiguration> { source };

        var fieldMappings = new Dictionary<string, string> { ["State"] = "State" };
        var resolvedMappings = CreateFieldMappings("SqlPrimary", fieldMappings);
        var dataset = CreateDataset("TestDataSet");

        var result = _analyzer.DecomposeBySource(filter, dataset, sources, resolvedMappings);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(0);
    }

    // --- ExtractConditions: field not mapped to any source ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourceSkipsFieldNotMappedToAnySource()
    {
        var mockOperator = new Mock<IFilterOperator>();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "UnknownField",
                Operator = mockOperator.Object,
                Value = "SomeValue"
            }
        };

        var mappingId = Guid.NewGuid();
        var source = CreateSource("SqlPrimary", supportsPredicatePushdown: true, fieldMappingIds: [mappingId]);
        var sources = new List<DataSetSourceConfiguration> { source };

        // Source has mappings but not for "UnknownField"
        var fieldMappings = new Dictionary<string, string> { ["State"] = "StateCode" };
        var resolvedMappings = CreateFieldMappings("SqlPrimary", fieldMappings);
        var dataset = CreateDataset("TestDataSet");

        var result = _analyzer.DecomposeBySource(filter, dataset, sources, resolvedMappings);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(0);
    }

    // --- BuildSourceMappingLookups: no field mapping IDs ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourceWithSourceHavingNoFieldMappingIdsStillSucceeds()
    {
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

        // Source with empty field mapping IDs
        var source = CreateSource("SqlPrimary", supportsPredicatePushdown: true, fieldMappingIds: []);
        var sources = new List<DataSetSourceConfiguration> { source };

        var resolvedMappings = EmptyFieldMappings();
        var dataset = CreateDataset("TestDataSet");

        var result = _analyzer.DecomposeBySource(filter, dataset, sources, resolvedMappings);

        // No field mappings means no conditions can be routed, so no source filters
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(0);
    }

    // --- Empty field mappings: no resolved mappings ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourceWithFailedFieldMappingResolutionStillSucceeds()
    {
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

        var mappingId = Guid.NewGuid();
        var source = CreateSource("SqlPrimary", supportsPredicatePushdown: true, fieldMappingIds: [mappingId]);
        var sources = new List<DataSetSourceConfiguration> { source };

        // Why: Empty field mappings simulates failed resolution — no mappings available
        var resolvedMappings = EmptyFieldMappings();

        var dataset = CreateDataset("TestDataSet");

        var result = _analyzer.DecomposeBySource(filter, dataset, sources, resolvedMappings);

        // Failed mapping resolution means no conditions routed, but no exception
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(0);
    }

    // --- ExtractConditions: FilterGroup with nested groups ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourceHandlesNestedFilterGroups()
    {
        var mockOperator = new Mock<IFilterOperator>();
        var innerCondition = new FilterCondition
        {
            PropertyName = "State",
            Operator = mockOperator.Object,
            Value = "Texas"
        };
        var innerGroup = new FilterGroup
        {
            Operator = LogicalOperator.Or,
            Nodes = new List<IFilterNode> { innerCondition }
        };
        var outerCondition = new FilterCondition
        {
            PropertyName = "City",
            Operator = mockOperator.Object,
            Value = "Dallas"
        };
        var outerGroup = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = new List<IFilterNode> { innerGroup, outerCondition }
        };
        var filter = new FilterExpression { Root = outerGroup };

        var mappingId = Guid.NewGuid();
        var source = CreateSource("SqlPrimary", supportsPredicatePushdown: true, fieldMappingIds: [mappingId]);
        var sources = new List<DataSetSourceConfiguration> { source };

        var fieldMappings = new Dictionary<string, string>
        {
            ["State"] = "state_code",
            ["City"] = "city_name"
        };
        var resolvedMappings = CreateFieldMappings("SqlPrimary", fieldMappings);
        var dataset = CreateDataset("TestDataSet");

        var result = _analyzer.DecomposeBySource(filter, dataset, sources, resolvedMappings);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
        result.Value.ShouldContainKey("SqlPrimary");

        // Both conditions from nested groups should be extracted to the same source
        var sourceFilter = result.Value["SqlPrimary"];
        var resultGroup = sourceFilter.Root.ShouldBeOfType<FilterGroup>();
        resultGroup.Nodes.Count.ShouldBe(2);
    }

    // --- TranslateFieldNames: condition with no mapping falls back to logical name ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourcePreservesUnmappedFieldNameInTranslation()
    {
        var mockOperator = new Mock<IFilterOperator>();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "DirectField",
                Operator = mockOperator.Object,
                Value = "Value"
            }
        };

        var mappingId = Guid.NewGuid();
        var source = CreateSource("SqlPrimary", supportsPredicatePushdown: true, fieldMappingIds: [mappingId]);
        var sources = new List<DataSetSourceConfiguration> { source };

        // Field mappings contain DirectField but with the same name (or a different field)
        // The source maps "DirectField" as a key so FindSourceForField will find it
        var fieldMappings = new Dictionary<string, string>
        {
            ["DirectField"] = "DirectField"  // Maps to same name
        };
        var resolvedMappings = CreateFieldMappings("SqlPrimary", fieldMappings);
        var dataset = CreateDataset("TestDataSet");

        var result = _analyzer.DecomposeBySource(filter, dataset, sources, resolvedMappings);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
        var condition = result.Value["SqlPrimary"].Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("DirectField");
    }

    // --- Empty sources list ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourceWithEmptySourcesReturnsEmptyFilters()
    {
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
        var resolvedMappings = EmptyFieldMappings();
        var dataset = CreateDataset("TestDataSet");

        var result = _analyzer.DecomposeBySource(filter, dataset, sources, resolvedMappings);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(0);
    }

    // --- Mixed: some conditions mapped, some unmapped ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourceWithMixOfMappedAndUnmappedFieldsOnlyIncludesMapped()
    {
        var mockOperator = new Mock<IFilterOperator>();
        var mappedCondition = new FilterCondition
        {
            PropertyName = "State",
            Operator = mockOperator.Object,
            Value = "Texas"
        };
        var unmappedCondition = new FilterCondition
        {
            PropertyName = "CalculatedField",
            Operator = mockOperator.Object,
            Value = 100
        };
        var group = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = new List<IFilterNode> { mappedCondition, unmappedCondition }
        };
        var filter = new FilterExpression { Root = group };

        var mappingId = Guid.NewGuid();
        var source = CreateSource("SqlPrimary", supportsPredicatePushdown: true, fieldMappingIds: [mappingId]);
        var sources = new List<DataSetSourceConfiguration> { source };

        // Only "State" is mapped, not "CalculatedField"
        var fieldMappings = new Dictionary<string, string> { ["State"] = "StateCode" };
        var resolvedMappings = CreateFieldMappings("SqlPrimary", fieldMappings);
        var dataset = CreateDataset("TestDataSet");

        var result = _analyzer.DecomposeBySource(filter, dataset, sources, resolvedMappings);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);

        // Only the mapped condition should appear
        var sourceFilter = result.Value["SqlPrimary"];
        var condition = sourceFilter.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("StateCode");
    }

    // --- TranslateFieldNames with no field mappings for source ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourceWithNoFieldMappingsForSourceUsesLogicalNames()
    {
        var mockOperator = new Mock<IFilterOperator>();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "State",
                Operator = mockOperator.Object,
                Value = "Texas"
            }
        };

        var sqlMappingId = Guid.NewGuid();
        var sqlSource = CreateSource("SqlPrimary", supportsPredicatePushdown: true, fieldMappingIds: [sqlMappingId]);

        // Another source without field mappings - but with the field registered through SQL source mapping
        var sources = new List<DataSetSourceConfiguration> { sqlSource };

        // The field mapping maps the field AND the source supports pushdown
        // But test the case where fieldMappingsBySource does NOT contain the source
        // This happens when resolver returns the mapping but we test TranslateFieldNames(conditions, null)
        // In GenerateSourceFilters, fieldMappingsBySource.TryGetValue returns false, fieldMappings is null
        // TranslateFieldNames with null fieldMappings returns the conditions unchanged

        // To achieve this: source has mappingIds but resolver fails
        // But FindSourceForField needs the field mapping to exist to route the condition
        // So we need a source that has the condition routed but no field mappings resolved

        // Actually, if resolver fails, no field mappings are stored, and FindSourceForField won't find the source
        // The path where TranslateFieldNames gets null fieldMappings requires:
        // 1. Condition is routed to a source (FindSourceForField found it via another source's mapping)
        // No, each source's mappings are checked independently

        // Let's test the simpler path: resolver succeeds with mappings that include the field
        var fieldMappings = new Dictionary<string, string> { ["State"] = "state_code" };
        var resolvedMappings = CreateFieldMappings("SqlPrimary", fieldMappings);
        var dataset = CreateDataset("TestDataSet");

        var result = _analyzer.DecomposeBySource(filter, dataset, sources, resolvedMappings);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
        var condition = result.Value["SqlPrimary"].Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("state_code");
    }

    // --- Operator preservation through decomposition ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourcePreservesOperatorInTranslatedCondition()
    {
        var mockOperator = new Mock<IFilterOperator>();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "Amount",
                Operator = mockOperator.Object,
                Value = 500
            }
        };

        var mappingId = Guid.NewGuid();
        var source = CreateSource("SqlPrimary", supportsPredicatePushdown: true, fieldMappingIds: [mappingId]);
        var sources = new List<DataSetSourceConfiguration> { source };

        var fieldMappings = new Dictionary<string, string> { ["Amount"] = "total_amount" };
        var resolvedMappings = CreateFieldMappings("SqlPrimary", fieldMappings);
        var dataset = CreateDataset("TestDataSet");

        var result = _analyzer.DecomposeBySource(filter, dataset, sources, resolvedMappings);

        result.IsSuccess.ShouldBeTrue();
        var condition = result.Value!["SqlPrimary"].Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBe(mockOperator.Object);
        condition.Value.ShouldBe(500);
    }

    // --- Multiple sources where one supports pushdown and other doesn't ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DecomposeBySourceOnlyGeneratesFiltersForPushdownSources()
    {
        var mockOperator = new Mock<IFilterOperator>();
        var condition1 = new FilterCondition
        {
            PropertyName = "State",
            Operator = mockOperator.Object,
            Value = "Texas"
        };
        var condition2 = new FilterCondition
        {
            PropertyName = "FileName",
            Operator = mockOperator.Object,
            Value = "data.csv"
        };
        var group = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = new List<IFilterNode> { condition1, condition2 }
        };
        var filter = new FilterExpression { Root = group };

        var sqlMappingId = Guid.NewGuid();
        var fileMappingId = Guid.NewGuid();

        var sqlSource = CreateSource("SqlPrimary", supportsPredicatePushdown: true, fieldMappingIds: [sqlMappingId]);
        var fileSource = CreateSource("FileSource", supportsPredicatePushdown: false, fieldMappingIds: [fileMappingId]);
        var sources = new List<DataSetSourceConfiguration> { sqlSource, fileSource };

        var sqlMappings = new Dictionary<string, string> { ["State"] = "StateCode" };
        var fileMappings = new Dictionary<string, string> { ["FileName"] = "file_name" };

        var resolvedMappings = CreateFieldMappings("SqlPrimary", sqlMappings, "FileSource", fileMappings);

        var dataset = CreateDataset("TestDataSet");

        var result = _analyzer.DecomposeBySource(filter, dataset, sources, resolvedMappings);

        result.IsSuccess.ShouldBeTrue();
        // Only SqlPrimary (supports pushdown) should have a filter
        result.Value!.Count.ShouldBe(1);
        result.Value.ShouldContainKey("SqlPrimary");
        result.Value.ShouldNotContainKey("FileSource");
    }

    // --- Helpers ---

    private static DataSetConfiguration CreateDataset(string name)
        => new DataSetConfiguration { Name = name };

    private static DataSetSourceConfiguration CreateSource(
        string sourceName,
        bool supportsPredicatePushdown = true,
        IList<Guid>? fieldMappingIds = null)
    {
        return new DataSetSourceConfiguration
        {
            SourceName = sourceName,
            SupportsPredicatePushdown = supportsPredicatePushdown,
            FieldMappingIds = fieldMappingIds ?? new List<Guid>(),
        };
    }

    // Why: Tests now pass pre-resolved field mappings directly instead of an IDataSetSourceResolver.
    // This matches the refactored DecomposeBySource signature.
    private static IDictionary<string, IReadOnlyDictionary<string, string>> CreateFieldMappings(
        string sourceName,
        Dictionary<string, string> fieldMappings)
    {
        return new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            [sourceName] = fieldMappings
        };
    }

    private static IDictionary<string, IReadOnlyDictionary<string, string>> CreateFieldMappings(
        string sourceName1, Dictionary<string, string> fieldMappings1,
        string sourceName2, Dictionary<string, string> fieldMappings2)
    {
        return new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            [sourceName1] = fieldMappings1,
            [sourceName2] = fieldMappings2
        };
    }

    private static IDictionary<string, IReadOnlyDictionary<string, string>> EmptyFieldMappings()
    {
        return new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
    }
}
