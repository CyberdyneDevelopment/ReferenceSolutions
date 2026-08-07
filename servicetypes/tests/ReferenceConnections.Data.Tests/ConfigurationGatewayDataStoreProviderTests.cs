using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Tests for <see cref="ConfigurationGatewayDataStoreProvider"/> — the server-side
/// <see cref="IDataStoreProvider"/> that adds the ConfigurationDb gateway shortcut/merge and the
/// Get(Guid id) name-resolution on top of the connection-agnostic <see cref="ConfiguredDataStoreProvider"/>
/// core. Pure config→selector→builder composition and dot-walk resolution now live entirely in
/// <see cref="ConfiguredDataStoreProvider"/> and are covered by
/// <c>Fdw.Data.DataNodes.Tests.ConfiguredDataStoreProviderTests</c> — this class only tests behavior
/// still owned here: ctor guards, the null-gateway delegation path, and Get(Guid id) resolution via
/// <see cref="DataStoreConfigurationProvider"/>.
/// </summary>
[Collection(nameof(DataServiceTestCollection))]
public sealed class ConfigurationGatewayDataStoreProviderTests
{
    private readonly Mock<ILogger<ConfigurationGatewayDataStoreProvider>> _mockLogger;
    private readonly Mock<IServiceConfigurationProvider<DataStoreConfiguration>> _mockCoreConfigProvider;
    private readonly Mock<IDataStoreBuilderSelector> _mockBuilderSelector;
    // Why: the real coreProvider ctor param is a concrete sealed class (cannot be Moq'd) — build a real
    // instance over mocked config/selector dependencies so its own branching stays unit-tested once, in
    // ConfiguredDataStoreProviderTests, while this class only exercises what IT adds.
    private readonly ConfiguredDataStoreProvider _coreProvider;
    // Why: DataStoreConfigurationProvider handles Get(Guid id) name resolution read through the gateway.
    private readonly DataStoreConfigurationProvider _dataStoreConfigProvider;
    private readonly ConfigurationGatewayDataStoreProvider _provider;

    public ConfigurationGatewayDataStoreProviderTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationGatewayDataStoreProvider>>();
        _mockCoreConfigProvider = new Mock<IServiceConfigurationProvider<DataStoreConfiguration>>();
        _mockBuilderSelector = new Mock<IDataStoreBuilderSelector>();
        _coreProvider = new ConfiguredDataStoreProvider(
            NullLogger<ConfiguredDataStoreProvider>.Instance,
            _mockCoreConfigProvider.Object,
            _mockBuilderSelector.Object);

        // Why: IDataGateway mock returns empty enumerables so DataStoreConfigurationProvider.Get()
        // does not NullReferenceException when the gateway is called during store lookups.
        var mockGateway = new Mock<Fdw.Services.Data.Abstractions.IConfigurationGateway>();
        mockGateway
            .Setup(g => g.Execute<System.Collections.Generic.IEnumerable<DataStoreConfiguration>>(
                It.IsAny<Fdw.Commands.Data.Abstractions.IDataCommand>(),
                It.IsAny<DataStoreTarget>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Fdw.Results.GenericResult<System.Collections.Generic.IEnumerable<DataStoreConfiguration>>.Success([]));
        // Why: the real gateway always exposes a non-null DataStores tree (used by FK parent-join
        // resolution during Get(id)); mirror that so the by-id path does not NRE on a null list.
        mockGateway.Setup(g => g.DataStores).Returns([]);
        _dataStoreConfigProvider = new DataStoreConfigurationProvider(NullLogger<DataStoreConfigurationProvider>.Instance, new Lazy<Fdw.Services.Data.Abstractions.IConfigurationGateway>(() => mockGateway.Object), BuildContainerProvider(mockGateway.Object));

        // Why: no top-level gateway wired — matches the gateway-less host configuration (reference-ui)
        // the core-extraction commit called out; Get(name)/Load fall through to _coreProvider entirely.
        _provider = new ConfigurationGatewayDataStoreProvider(
            _mockLogger.Object,
            _coreProvider,
            _dataStoreConfigProvider);
    }

    // Why: DataStoreConfigurationProvider requires a container provider for AddContainer.
    // These tests never call AddContainer, but the dependency is required (no shim) — build a
    // real Lazy over the same mock gateway; it is never forced here.
    private static Lazy<DefaultConfigurationProvider<DataContainerConfiguration, Fdw.Services.Connections.Commands.DataContainerConfigurationCommand>> BuildContainerProvider(
        Fdw.Services.Data.Abstractions.IConfigurationGateway gateway)
    {
        return new Lazy<DefaultConfigurationProvider<DataContainerConfiguration, Fdw.Services.Connections.Commands.DataContainerConfigurationCommand>>(
            () => new DefaultConfigurationProvider<DataContainerConfiguration, Fdw.Services.Connections.Commands.DataContainerConfigurationCommand>(
                NullLogger<DefaultConfigurationProvider<DataContainerConfiguration, Fdw.Services.Connections.Commands.DataContainerConfigurationCommand>>.Instance,
                new Lazy<Fdw.Services.Data.Abstractions.IConfigurationGateway>(() => gateway),
                "ConfigurationDb",
                "data"));
    }

    // ====================================================================
    // Constructor validation
    // ====================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorShouldNotThrowWhenLoggerIsNull()
    {
        // Why: ConfigurationGatewayDataStoreProvider uses NullLogger<T>.Instance as fallback for null
        // logger — constructing with null logger is valid and does not throw.
        // Act & Assert
        Should.NotThrow(() => new ConfigurationGatewayDataStoreProvider(
            null!,
            _coreProvider,
            _dataStoreConfigProvider));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorShouldNotThrowWhenGatewayIsNull()
    {
        // Why: gateway is an optional Lazy<IConfigurationGateway>? — gateway-less hosts (reference-ui)
        // construct this provider with no gateway at all.
        // Act & Assert
        Should.NotThrow(() => new ConfigurationGatewayDataStoreProvider(
            _mockLogger.Object,
            _coreProvider,
            _dataStoreConfigProvider,
            gateway: null));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorShouldThrowWhenCoreProviderIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ConfigurationGatewayDataStoreProvider(
            _mockLogger.Object,
            null!,
            _dataStoreConfigProvider));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorShouldThrowWhenDataStoreConfigProviderIsNull()
    {
        Should.Throw<ArgumentNullException>(() => new ConfigurationGatewayDataStoreProvider(
            _mockLogger.Object,
            _coreProvider,
            null!));
    }

    // ====================================================================
    // Get(name) — no gateway configured, delegates entirely to the connection-agnostic core.
    // Name validation and "not found" branching are the core's OWN logic and are exhaustively
    // covered by ConfiguredDataStoreProviderTests; this is a single wiring/regression check that the
    // null-gateway delegation path (used by gateway-less hosts) actually reaches the core and
    // propagates its result.
    // ====================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetDataStoreShouldReturnFailureWhenDataStoreNotFound()
    {
        // Arrange
        _mockCoreConfigProvider
            .Setup(p => p.Get("NonExistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Failure(new GenericMessage("not found")));

        // Act
        var result = await _provider.Get("NonExistent", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    // ====================================================================
    // Get() / Load — no gateway configured, exercises MergeConfigurationGatewayDataStores' own
    // null-gateway early-return branch (code unique to this class).
    // ====================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetAllDataStoresShouldReturnEmptyListWhenNoDataStoresRegistered()
    {
        // Arrange
        _mockCoreConfigProvider
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataStoreConfiguration>>.Success([]));

        // Act
        var result = await _provider.Get(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.ShouldBeEmpty();
    }

    // ====================================================================
    // Get(Guid id) — resolution via DataStoreConfigurationProvider (behavior owned by this class)
    // ====================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetDataStoreByIdReturnsFailureWhenNotFound()
    {
        var result = await _provider.Get(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
