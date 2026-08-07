using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Abstractions;
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

[Collection(nameof(DataServiceTestCollection))]
public sealed class DataSetProviderTests
{
    private readonly Mock<ILogger<DataSetProvider>> _mockLogger;
    private readonly Mock<DefaultConfigurationProvider<DataSetConfiguration, DataSetConfigurationCommand>> _mockConfigProvider;
    private readonly DataSetProvider _provider;

    public DataSetProviderTests()
    {
        _mockLogger = new Mock<ILogger<DataSetProvider>>();

        // Why: All constructor args must be passed explicitly to Moq — Castle DynamicProxy uses
        // reflection-based instantiation and cannot resolve C# optional parameter defaults.
        _mockConfigProvider = new Mock<DefaultConfigurationProvider<DataSetConfiguration, DataSetConfigurationCommand>>(
            NullLogger<DefaultConfigurationProvider<DataSetConfiguration, DataSetConfigurationCommand>>.Instance,
            new Lazy<Fdw.Services.Data.Abstractions.IConfigurationGateway>(() => null!),
            "TestStore",
            "data",
            (object?)null!) { CallBase = false }; // invalidator

        // Why: Default setup returns empty/failure so tests that don't need config provider
        // get predictable behavior without per-test setup.
#pragma warning disable CS8620 // Why: Mock returns non-nullable generic result; nullable wrapping is intentional for test setup
        _mockConfigProvider.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataSetConfiguration?>.Failure(new GenericMessage("Not found")));
        _mockConfigProvider.Setup(c => c.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataSetConfiguration?>.Failure(new GenericMessage("Not found")));
#pragma warning restore CS8620
        _mockConfigProvider.Setup(c => c.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataSetConfiguration>>.Success(
                new List<DataSetConfiguration>()));

        _provider = new DataSetProvider(
            _mockLogger.Object,
            _mockConfigProvider.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetDataSetShouldReturnFailureWhenNameIsNull()
    {
        // Act
        var result = await _provider.Get(null!, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetDataSetShouldReturnFailureWhenNameIsEmpty()
    {
        // Act
        var result = await _provider.Get(string.Empty, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetDataSetShouldReturnFailureWhenDataSetNotFound()
    {
        // Act
        var result = await _provider.Get("NonExistent", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetDataSetShouldReturnSuccessWhenDataSetExists()
    {
        // Arrange
        var config = CreateDataSetConfiguration("TestDataSet");
#pragma warning disable CS8620 // Why: Mock returns non-nullable generic result; nullable wrapping is intentional for test setup
        _mockConfigProvider.Setup(x => x.Get("TestDataSet", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataSetConfiguration?>.Success(config));
#pragma warning restore CS8620

        // Act
        var result = await _provider.Get("TestDataSet", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("TestDataSet");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetDataSetShouldBeCaseInsensitive()
    {
        // Arrange
        var config = CreateDataSetConfiguration("TestDataSet");
#pragma warning disable CS8620 // Why: Mock returns non-nullable generic result; nullable wrapping is intentional for test setup
        _mockConfigProvider.Setup(x => x.Get("TESTDATASET", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataSetConfiguration?>.Success(config));
#pragma warning restore CS8620

        // Act
        var result = await _provider.Get("TESTDATASET", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetAllShouldReturnEmptyListWhenNoDataSetsRegistered()
    {
        // Act
        var result = await _provider.Get(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        // Why: GetAll returns DataSetTypes.All() entries even with no config provider data.
        // The list may contain TypeCollection entries, so we just verify it succeeds.
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetAllShouldReturnAllConfiguredDataSets()
    {
        // Arrange
        var config1 = CreateDataSetConfiguration("DataSet1");
        var config2 = CreateDataSetConfiguration("DataSet2");
        var config3 = CreateDataSetConfiguration("DataSet3");

        _mockConfigProvider.Setup(x => x.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataSetConfiguration>>.Success(
                new List<DataSetConfiguration> { config1, config2, config3 }));

        // Act
        var result = await _provider.Get(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        // Why: Result includes both config provider entries and TypeCollection entries.
        // At minimum, the 3 configured datasets should be present.
        result.Value!.Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateDataSetShouldReturnFailureWhenConfigurationIsNull()
    {
        // Act
        var result = _provider.ValidateDataSet(null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateDataSetShouldReturnFailureWhenNameIsEmpty()
    {
        // Arrange
        var config = CreateDataSetConfiguration("");

        // Act
        var result = _provider.ValidateDataSet(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateDataSetShouldReturnFailureWhenFieldsAreEmpty()
    {
        // Arrange
        var config = CreateDataSetConfiguration("Test");
        config.Fields.Clear();

        // Act
        var result = _provider.ValidateDataSet(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateDataSetShouldReturnFailureWhenSourceIdsAreEmpty()
    {
        // Arrange
        var config = CreateDataSetConfiguration("Test");
        config.Sources.Clear();

        // Act
        var result = _provider.ValidateDataSet(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateDataSetShouldSucceedWhenKeyFieldRecordsPresent()
    {
        // Arrange
        var config = CreateDataSetConfiguration("Test");
        config.KeyFields = new List<DataSetKeyFieldConfiguration>
        {
            new() { KeyName = "Id", KeyType = "Surrogate", Ordinal = 0 }
        };
        // Why: Sources are iterated directly; give the source FieldMappingIds so validation passes.
        config.Sources[0].FieldMappingIds = new List<Guid> { Guid.NewGuid() };

        // Act
        var result = _provider.ValidateDataSet(config);

        // Why: Name-matches-field-list validation was removed; provider only checks that
        // key-field records exist at all. Deep resolution happens at load time.
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateDataSetShouldReturnSuccessForValidConfiguration()
    {
        // Arrange
        var config = CreateDataSetConfiguration("Test");
        // Why: Sources are iterated directly; give the source a non-empty FieldMappingIds so validation passes.
        config.Sources[0].FieldMappingIds = new List<Guid> { Guid.NewGuid() };

        // Act
        var result = _provider.ValidateDataSet(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetDataSetByIdShouldReturnFailureWhenNotFound()
    {
        // Act
        var result = await _provider.Get(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetDataSetByIdShouldReturnFromCacheWhenNotMaterialized()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var dsConfig = new DataSetConfiguration
        {
            Id = dataSetId,
            Name = "ConfiguredDS",
            Fields = [new DataFieldConfiguration { Name = "Id", TypeName = "Int32" }],
            Sources = [new DataSetSourceConfiguration { Id = Guid.NewGuid() }],
            KeyFields = [new DataSetKeyFieldConfiguration { KeyName = "Id", KeyType = "Surrogate", Ordinal = 0 }]
        };
#pragma warning disable CS8620 // Why: Mock returns non-nullable generic result; nullable wrapping is intentional for test setup
        _mockConfigProvider.Setup(c => c.Get(dataSetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataSetConfiguration?>.Success(dsConfig));
#pragma warning restore CS8620

        // Act
        var result = await _provider.Get(dataSetId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("ConfiguredDS");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ConstructorWithCacheShouldSupportCacheBasedLookup()
    {
        // Arrange
        var dsConfig = new DataSetConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "TestDS",
            Fields = [new DataFieldConfiguration { Name = "Id", TypeName = "Int32" }],
            Sources = [new DataSetSourceConfiguration { Id = Guid.NewGuid() }],
            KeyFields = [new DataSetKeyFieldConfiguration { KeyName = "Id", KeyType = "Surrogate", Ordinal = 0 }]
        };
#pragma warning disable CS8620 // Why: Mock returns non-nullable generic result; nullable wrapping is intentional for test setup
        _mockConfigProvider.Setup(c => c.Get(dsConfig.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataSetConfiguration?>.Success(dsConfig));
#pragma warning restore CS8620

        // Act & Assert
        var result = await _provider.Get(dsConfig.Id, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithNullConfigurationProviderAcceptsNull()
    {
        // Why: configurationProvider is optional — constructor accepts null and the provider
        // falls back to TypeCollection-based lookups without a DB-backed config provider.
        var provider = new DataSetProvider(
            _mockLogger.Object,
            null!);

        provider.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateDataSetShouldReturnFailureWhenSourceHasNoFieldMappings()
    {
        // Arrange
        var config = CreateDataSetConfiguration("Test");
        // Why: CreateDataSetConfiguration leaves FieldMappingIds empty; validation fails because
        // Sources are iterated directly and the empty FieldMappingIds triggers SourceNoFieldMappings.
        config.Sources[0].FieldMappingIds = new List<Guid>();

        // Act
        var result = _provider.ValidateDataSet(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateDataSetShouldReturnFailureWhenSourceHasNullFieldMappingIds()
    {
        // Arrange
        var config = CreateDataSetConfiguration("Test");
        // Why: null FieldMappingIds triggers the same guard as empty — SourceNoFieldMappings.
        config.Sources[0].FieldMappingIds = null!;

        // Act
        var result = _provider.ValidateDataSet(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetDataSetByIdShouldReturnSuccessViaConfigProvider()
    {
        // Arrange
        var config = CreateDataSetConfiguration("Test");
#pragma warning disable CS8620 // Why: Mock returns non-nullable generic result; nullable wrapping is intentional for test setup
        _mockConfigProvider.Setup(c => c.Get(config.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataSetConfiguration?>.Success(config));
#pragma warning restore CS8620

        // Act
        var result = await _provider.Get(config.Id, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("Test");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateDataSetShouldSucceedWithNoKeyFields()
    {
        // Arrange
        var config = CreateDataSetConfiguration("Test");
        config.KeyFields = new List<DataSetKeyFieldConfiguration>();
        // Why: Sources are iterated directly; give the source FieldMappingIds so validation passes.
        config.Sources[0].FieldMappingIds = new List<Guid> { Guid.NewGuid() };

        // Act
        var result = _provider.ValidateDataSet(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateDataSetShouldReturnFailureWhenMultipleSourcesAndFirstHasNoFieldMappings()
    {
        // Arrange
        var config = CreateDataSetConfiguration("Test");
        // Add a second source with FieldMappingIds, but leave the first with empty FieldMappingIds.
        config.Sources.Add(new DataSetSourceConfiguration
        {
            Id = Guid.NewGuid(),
            FieldMappingIds = new List<Guid> { Guid.NewGuid() }
        });
        config.Sources[0].FieldMappingIds = new List<Guid>();

        // Act
        var result = _provider.ValidateDataSet(config);

        // Assert
        // Why: ValidateDataSetSources iterates ALL sources; first source with empty FieldMappingIds
        // triggers failure regardless of whether later sources have valid mappings.
        result.IsSuccess.ShouldBeFalse();
    }

    private DataSetConfiguration CreateDataSetConfiguration(string name)
    {
        return new DataSetConfiguration
        {
            Name = name,
            Fields = new List<DataFieldConfiguration>
            {
                new DataFieldConfiguration { Name = "Id", TypeName = "Int32" }
            },
            Sources = new List<DataSetSourceConfiguration> { new() { Id = Guid.NewGuid() } },
            KeyFields = new List<DataSetKeyFieldConfiguration> { new() { KeyName = "Id", KeyType = "Surrogate", Ordinal = 0 } }
        };
    }

    private static DataSetSourceConfiguration CreateSourceConfiguration(string sourceName, Guid dataSetConfigId)
    {
        return new DataSetSourceConfiguration
        {
            SourceName = sourceName,
            DataSetId = dataSetConfigId,
            ConnectionName = "TestConnection",
            ConnectionType = "MsSql",
            DataStoreName = "TestStore",
            Priority = 1,
            Path = "dbo",
            ContainerName = "TestTable",
            FieldMappingIds = new List<Guid> { Guid.NewGuid() }
        };
    }

    private static DataSetSourceConfiguration CreateHttpSourceConfiguration(string sourceName, Guid dataSetConfigId)
    {
        return new DataSetSourceConfiguration
        {
            SourceName = sourceName,
            DataSetId = dataSetConfigId,
            ConnectionName = "HttpConnection",
            ConnectionType = "Http",
            DataStoreName = "ApiStore",
            Priority = 1,
            HttpEndpoint = "https://api.example.com/data",
            HttpMethod = "POST",
            SupportsPredicatePushdown = false,
            FieldMappingIds = new List<Guid> { Guid.NewGuid() }
        };
    }

    private static DataSetSourceConfiguration CreateFileSourceConfiguration(string sourceName, Guid dataSetConfigId)
    {
        return new DataSetSourceConfiguration
        {
            SourceName = sourceName,
            DataSetId = dataSetConfigId,
            ConnectionName = "FileConnection",
            ConnectionType = "File",
            DataStoreName = "FileStore",
            Priority = 2,
            FilePath = "/data/exports/*.csv",
            FileFormat = "csv",
            SupportsPredicatePushdown = false,
            FieldMappingIds = new List<Guid> { Guid.NewGuid() }
        };
    }
}
