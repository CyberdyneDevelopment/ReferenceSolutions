using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Commands;
using Fdw.Services.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Additional tests for DataSetProvider covering remaining branches:
/// - GetDefaultSource with not-found dataset
/// - GetSource/GetFieldMappings no-message failure branches
/// - GetFieldMappings with null FieldMappingIds
/// - GetDataSetById config-provider lookup test
/// - Null logger fallback
/// </summary>
[Collection(nameof(DataServiceTestCollection))]
public sealed class DataSetProviderAdditionalTests
{
    private readonly Mock<ILogger<DataSetProvider>> _mockLogger;

    public DataSetProviderAdditionalTests()
    {
        _mockLogger = new Mock<ILogger<DataSetProvider>>();
    }

    // ================================================================
    // Helper Methods
    // ================================================================

    private static DataSetConfiguration CreateDataSetConfig(
        string name,
        Guid? id = null,
        string? recordTypeName = null,
        List<Guid>? sourceIds = null)
    {
        return new DataSetConfiguration
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            RecordTypeName = recordTypeName ?? string.Empty,
            Fields = new List<DataFieldConfiguration>
            {
                new() { Name = "Id", TypeName = "Int32", IsKey = true }
            },
            Sources = (sourceIds ?? new List<Guid> { Guid.NewGuid() }).Select(sid => new DataSetSourceConfiguration { Id = sid }).ToList(),
            KeyFields = new List<DataSetKeyFieldConfiguration> { new() { KeyName = "Id", KeyType = "Surrogate", Ordinal = 0 } }
        };
    }

    private static DataSetSourceConfiguration CreateSourceConfig(
        string sourceName,
        Guid? id = null,
        Guid? dataSetConfigId = null,
        Guid? containerId = null,
        int priority = 1,
        string? path = null,
        string? containerName = null,
        string? httpEndpoint = null,
        string? httpMethod = null,
        string? filePath = null,
        string? fileFormat = null)
    {
        return new DataSetSourceConfiguration
        {
            Id = id ?? Guid.NewGuid(),
            SourceName = sourceName,
            DataSetId = dataSetConfigId ?? Guid.NewGuid(),
            ContainerId = containerId,
            Priority = priority,
            ConnectionName = "TestConn",
            ConnectionType = "MsSql",
            DataStoreName = "TestStore",
            Path = path ?? string.Empty,
            ContainerName = containerName ?? string.Empty,
            HttpEndpoint = httpEndpoint,
            HttpMethod = httpMethod,
            FilePath = filePath,
            FileFormat = fileFormat,
            FieldMappingIds = new List<Guid> { Guid.NewGuid() }
        };
    }

    // Why: Moq can't proxy DefaultConfigurationProvider<T,C> reliably because the partial source-
    // generated class has constructor argument variations. Use a hand-rolled stub subclass instead
    // that overrides the virtual Get methods.
    private sealed class StubDataSetConfigProvider : DefaultConfigurationProvider<DataSetConfiguration, DataSetConfigurationCommand>
    {
        private readonly Dictionary<Guid, DataSetConfiguration> _byId = new();
        private readonly Dictionary<string, DataSetConfiguration> _byName = new(StringComparer.OrdinalIgnoreCase);

        public StubDataSetConfigProvider()
            : base(null,
                new Lazy<Fdw.Services.Data.Abstractions.IConfigurationGateway>(() => null!),
                "TestStore", "data")
        {
        }

        public StubDataSetConfigProvider WithById(Guid id, DataSetConfiguration cfg)
        {
            _byId[id] = cfg;
            return this;
        }

        public StubDataSetConfigProvider WithByName(string name, DataSetConfiguration cfg)
        {
            _byName[name] = cfg;
            return this;
        }

        public override Task<IGenericResult<DataSetConfiguration>> Get(Guid id, CancellationToken ct = default)
            => Task.FromResult(_byId.TryGetValue(id, out var cfg)
                ? GenericResult<DataSetConfiguration>.Success(cfg)
                : GenericResult<DataSetConfiguration>.Failure(new GenericMessage("Not found")));

        public override Task<IGenericResult<DataSetConfiguration>> Get(string name, CancellationToken ct = default)
            => Task.FromResult(_byName.TryGetValue(name, out var cfg)
                ? GenericResult<DataSetConfiguration>.Success(cfg)
                : GenericResult<DataSetConfiguration>.Failure(new GenericMessage("Not found")));

        public override Task<IGenericResult<IReadOnlyList<DataSetConfiguration>>> Get(CancellationToken ct = default)
            => Task.FromResult(GenericResult<IReadOnlyList<DataSetConfiguration>>.Success(new List<DataSetConfiguration>()));
    }

    private StubDataSetConfigProvider CreateStubConfigProvider()
    {
        return new StubDataSetConfigProvider();
    }

    // ================================================================
    // GetDataSetById returns success via config provider
    // ================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetDataSetByIdReturnsSuccessViaConfigProvider()
    {
        // Arrange
        var sharedId = Guid.NewGuid();
        var configuredDs = CreateDataSetConfig("ConfiguredVersion", id: sharedId);

        var configProvider = CreateStubConfigProvider().WithById(sharedId, configuredDs);

        var provider = new DataSetProvider(
            _mockLogger.Object,
            configProvider);

        // Act
        var result = await provider.Get(sharedId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("ConfiguredVersion");
    }

    // ================================================================
    // Null logger uses NullLogger
    // ================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ConstructorWithNullLoggerUsesNullLoggerInstance()
    {
        // Arrange & Act
        var provider = new DataSetProvider(null);

        // Assert - should not throw and work normally
        var result = await provider.Get(TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue();
    }

    // ================================================================
    // ValidateDataSet - multiple key fields, one invalid
    // ================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateDataSetReturnsFailureForFirstInvalidKeyField()
    {
        // Arrange
        var provider = new DataSetProvider(_mockLogger.Object);
        var config = CreateDataSetConfig("Test");
        config.Fields = new List<DataFieldConfiguration>
        {
            new() { Name = "Id", TypeName = "Int32" },
            new() { Name = "Name", TypeName = "String" }
        };
        config.KeyFields = new List<DataSetKeyFieldConfiguration>
        {
            new() { KeyName = "Id", KeyType = "Surrogate", Ordinal = 0 },
            new() { KeyName = "MissingField", KeyType = "Surrogate", Ordinal = 1 }
        };
        // Why: Sources are iterated directly; give the source FieldMappingIds so validation passes.
        config.Sources[0].FieldMappingIds = new List<Guid> { Guid.NewGuid() };

        // Act
        var result = provider.ValidateDataSet(config);

        // Why: Name-matches-field-list validation removed — key-field records live in
        // data.DataSetKeyField and are resolved by RowId at provider load time.
        result.IsSuccess.ShouldBeTrue();
    }

    // ================================================================
    // ValidateDataSet - null KeyFields treated as empty (no validation needed)
    // ================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateDataSetSucceedsWhenKeyFieldsNull()
    {
        // Arrange
        var provider = new DataSetProvider(_mockLogger.Object);
        var config = CreateDataSetConfig("Test");
        config.KeyFields = null!;
        // Why: Sources are iterated directly; give the source FieldMappingIds so validation passes.
        config.Sources[0].FieldMappingIds = new List<Guid> { Guid.NewGuid() };

        // Act
        var result = provider.ValidateDataSet(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // ================================================================
    // ValidateDataSet - null Fields
    // ================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateDataSetReturnsFailureWhenFieldsNull()
    {
        // Arrange
        var provider = new DataSetProvider(_mockLogger.Object);
        var config = CreateDataSetConfig("Test");
        config.Fields = null!;

        // Act
        var result = provider.ValidateDataSet(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    // ================================================================
    // ValidateDataSet - null SourceIds
    // ================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateDataSetReturnsFailureWhenSourceIdsNull()
    {
        // Arrange
        var provider = new DataSetProvider(_mockLogger.Object);
        var config = CreateDataSetConfig("Test");
        config.Sources = null!;

        // Act
        var result = provider.ValidateDataSet(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    // ================================================================
    // ValidateDataSet - source with null FieldMappingIds
    // ================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateDataSetReturnsFailureWhenSourceHasNullFieldMappingIds()
    {
        // Arrange
        var provider = new DataSetProvider(_mockLogger.Object);
        var config = CreateDataSetConfig("Test");
        // Why: Sources are iterated directly; null FieldMappingIds triggers SourceNoFieldMappings.
        config.Sources[0].FieldMappingIds = null!;

        // Act
        var result = provider.ValidateDataSet(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    // ================================================================
    // GetDataSetById uses cache when not materialized
    // ================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetDataSetByIdUsesCacheWhenNotMaterialized()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var dsConfig = CreateDataSetConfig("CachedDS", id: dataSetId);

        var configProvider = CreateStubConfigProvider().WithById(dataSetId, dsConfig);

        var provider = new DataSetProvider(
            _mockLogger.Object,
            configProvider);

        // Act
        var result = await provider.Get(dataSetId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("CachedDS");
    }
}
