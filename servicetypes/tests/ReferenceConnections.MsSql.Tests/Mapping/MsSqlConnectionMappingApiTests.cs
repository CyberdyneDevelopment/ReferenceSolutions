using System.Reflection;
using ReferenceConnections.MsSql.Mapping;
using Shouldly;

namespace ReferenceConnections.MsSql.Tests.Mapping;

/// <summary>
/// Tests for the MsSqlConnection mapping API structure and contracts.
/// These tests verify the API surface without requiring an actual SqlDataReader.
/// </summary>
/// <remarks>
/// Full integration tests with real SqlDataReader require database connectivity
/// and are documented in the Integration Tests section.
/// </remarks>
public sealed class MsSqlConnectionMappingApiTests
{
    #region API Surface Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateMappingContextMethodExists()
    {
        // Arrange
        var type = typeof(MsSqlConnection);

        // Act
        var method = type.GetMethod("CreateMappingContext", BindingFlags.Static | BindingFlags.NonPublic);

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(RowMappingContext));
        method.GetParameters().Length.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapReaderRowToDictionaryWithContextMethodExists()
    {
        // Arrange
        var type = typeof(MsSqlConnection);

        // Act - Get the context-based overload (legacy overload was removed)
        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Where(m => m.Name == "MapReaderRowToDictionary")
            .ToList();

        // Assert - Should have 1 overload: (reader, context)
        methods.Count.ShouldBe(1);

        var contextOverload = methods.FirstOrDefault(m =>
            m.GetParameters().Any(p => p.ParameterType == typeof(RowMappingContext)));
        contextOverload.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnDictionaryMethodExists()
    {
        // Arrange
        var type = typeof(MsSqlConnection);

        // Act
        var method = type.GetMethod("ReturnDictionary", BindingFlags.Static | BindingFlags.NonPublic);

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(void));
        method.GetParameters().Length.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void LegacyOverloadWasRemoved()
    {
        // Arrange
        var type = typeof(MsSqlConnection);

        // Act - Verify the legacy overload (reader, container) no longer exists
        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Where(m => m.Name == "MapReaderRowToDictionary")
            .ToList();

        var legacyOverload = methods.FirstOrDefault(m =>
            m.GetParameters().All(p => p.ParameterType != typeof(RowMappingContext)));

        // Assert - Legacy overload was replaced by context-based overload
        legacyOverload.ShouldBeNull();
    }

    #endregion

    #region RowMappingContext Structure Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RowMappingContextHasExpectedFields()
    {
        // Arrange
        var type = typeof(RowMappingContext);

        // Assert - Verify public readonly fields exist
        var fieldOrdinals = type.GetField("FieldOrdinals");
        fieldOrdinals.ShouldNotBeNull();

        var converters = type.GetField("Converters");
        converters.ShouldNotBeNull();

        var fieldNames = type.GetField("FieldNames");
        fieldNames.ShouldNotBeNull();

        var fieldCount = type.GetField("FieldCount");
        fieldCount.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RowMappingContextCreateMethodExists()
    {
        // Arrange
        var type = typeof(RowMappingContext);

        // Act
        var method = type.GetMethod("Create", BindingFlags.Static | BindingFlags.Public);

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(RowMappingContext));
    }

    #endregion

    #region Integration Test Documentation

    /// <summary>
    /// Integration tests require actual database connectivity.
    /// The following scenarios should be tested with a real SqlDataReader:
    ///
    /// 1. CreateCachesConvertersOnce - Verify MsSqlConverters.All().ToDictionary() is called once
    /// 2. CreateHandlesMissingFields - Verify ordinal = -1 for missing fields
    /// 3. CreatePrecomputesOrdinals - Verify ordinals are cached correctly
    /// 4. MapRowUsesPrecomputedOrdinals - Verify no GetOrdinal calls during mapping
    /// 5. MapRowAppliesConverters - Verify converters are applied correctly
    /// 6. MapRowHandlesNullValues - Verify DBNull handling
    /// 7. ContextOverloadIsFaster - Benchmark comparison with legacy overload
    /// </summary>
    [Fact(Skip = "Documentation test - describes required integration tests")]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IntegrationTestDocumentation()
    {
        // This test documents what integration tests are needed.
        // Integration tests should be added when database connectivity is available.
    }

    #endregion
}
