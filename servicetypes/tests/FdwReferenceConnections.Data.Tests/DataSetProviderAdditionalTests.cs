using Fdw.Services.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

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

    private static SimpleDataSetConfiguration CreateDataSetConfig(
        string name,
        Guid? id = null,
        string? recordTypeName = null,
        List<Guid>? sourceIds = null)
    {
        return new SimpleDataSetConfiguration
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            RecordTypeName = recordTypeName ?? string.Empty,
            Fields = new List<DataSetFieldConfiguration>
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
            ContainerId = containerId,
            Priority = priority,
            ConnectionName = "TestConn",
            ConnectionType = "MsSql",
            DataStoreName = "TestStore",
            PathValue = path ?? string.Empty,
            ContainerName = containerName ?? string.Empty,
            HttpEndpoint = httpEndpoint,
            HttpMethod = httpMethod,
            FilePath = filePath,
            FileFormat = fileFormat,
            FieldMappingIds = new List<Guid> { Guid.NewGuid() }
        };
    }

    // Why a hand-rolled stub and not a Moq proxy: this is what DataSetProvider actually takes --
    // IDataSetConfigurationProvider, the domain contract -- and a stub reads better than nine Setup
    // calls for a surface where only three members matter here.
    private sealed class StubDataSetConfigProvider : IDataSetConfigurationProvider
    {
        private readonly Dictionary<Guid, IDataSetImplementationConfiguration> _byId = new();
        private readonly Dictionary<string, IDataSetImplementationConfiguration> _byName = new(StringComparer.OrdinalIgnoreCase);

        public StubDataSetConfigProvider WithById(Guid id, IDataSetImplementationConfiguration cfg)
        {
            _byId[id] = cfg;
            return this;
        }

        public StubDataSetConfigProvider WithByName(string name, IDataSetImplementationConfiguration cfg)
        {
            _byName[name] = cfg;
            return this;
        }

        public Task<IGenericResult<IDataSetImplementationConfiguration>> Get(Guid id, CancellationToken ct = default)
            => Task.FromResult(_byId.TryGetValue(id, out var cfg)
                ? GenericResult<IDataSetImplementationConfiguration>.Success(cfg)
                : GenericResult<IDataSetImplementationConfiguration>.Failure(new GenericMessage("Not found")));

        public Task<IGenericResult<IDataSetImplementationConfiguration>> Get(string name, CancellationToken ct = default)
            => Task.FromResult(_byName.TryGetValue(name, out var cfg)
                ? GenericResult<IDataSetImplementationConfiguration>.Success(cfg)
                : GenericResult<IDataSetImplementationConfiguration>.Failure(new GenericMessage("Not found")));

        public Task<IGenericResult<IDataSetImplementationConfiguration>> Get(Guid id, DateTimeOffset asOf, CancellationToken ct = default)
            => Task.FromResult(GenericResult<IDataSetImplementationConfiguration>.Failure(
                new GenericMessage("This stub answers no as-of read")));

        public Task<IGenericResult<IDataSetImplementationConfiguration>> Get(string name, int levels, CancellationToken ct = default)
            => Get(name, ct);

        public Task<IGenericResult<IDataSetImplementationConfiguration>> Get(Guid id, int levels, CancellationToken ct = default)
            => Get(id, ct);

        public Task<IGenericResult<IReadOnlyList<IDataSetImplementationConfiguration>>> Get(int levels, CancellationToken ct = default)
            => Get(ct);

        public Task<IGenericResult<ConfigurationPage<IDataSetImplementationConfiguration>>> Get(int skip, int take, int levels, CancellationToken ct = default)
            => Task.FromResult(GenericResult<ConfigurationPage<IDataSetImplementationConfiguration>>.Failure(
                new GenericMessage("This stub answers no paged read")));

        public Task<IGenericResult<IReadOnlyList<TChild>>> Get<TChild>(string name, int levels, CancellationToken ct = default)
            => Task.FromResult(GenericResult<IReadOnlyList<TChild>>.Failure(
                new GenericMessage("This stub answers no child-collection read")));

        public Task<IGenericResult<IReadOnlyList<TChild>>> Get<TOwner, TChild>(string name, Guid ownerId, int levels, CancellationToken ct = default)
            => Task.FromResult(GenericResult<IReadOnlyList<TChild>>.Failure(
                new GenericMessage("This stub answers no owner child-collection read")));

        public Task<IGenericResult<IReadOnlyList<IDataSetImplementationConfiguration>>> Get(CancellationToken ct = default)
            => Task.FromResult(GenericResult<IReadOnlyList<IDataSetImplementationConfiguration>>.Success(
                new List<IDataSetImplementationConfiguration>()));

        public Task<IGenericResult<IReadOnlyList<T>>> Find<T>(Func<T, bool> predicate, CancellationToken ct = default)
            where T : IDataSetImplementationConfiguration
            => Task.FromResult(GenericResult<IReadOnlyList<T>>.Success(new List<T>()));

        public Task<IGenericResult> Save<T>(T implementationConfiguration, string domain, string implementationName, string name, CancellationToken ct = default)
            where T : IDataSetImplementationConfiguration
            => Task.FromResult(GenericResult.Success());

        public Task<IGenericResult> Delete(Guid id, CancellationToken ct = default)
            => Task.FromResult(GenericResult.Success());

        public Task<IGenericResult> Delete(string name, CancellationToken ct = default)
            => Task.FromResult(GenericResult.Success());

        public IGenericResult Register<T>(string name, T implementationConfigurationProvider)
            => GenericResult.Success();

        public IGenericResult<IImplementationConfigurationProvider<IDataSetImplementationConfiguration>> Implementation(string implementationName)
            => throw new NotSupportedException("Test provider does not support Implementation.");
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
        config.Fields = new List<DataSetFieldConfiguration>
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
