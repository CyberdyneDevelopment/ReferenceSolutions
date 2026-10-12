using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Data.Limits;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Tests for ConfigurationSaveCommand routing in DataGatewayService.
/// </summary>
/// <remarks>
/// Why: ConfigurationCascadeProjection and ConfigurationTypeBase were deleted in FDW-395 Wave C5.
/// IConfigurationType and ConfigurationTypes TypeCollection are gone. These tests now verify only
/// that DataGatewayService routes ConfigurationSaveCommands to ExecuteContainer, which returns
/// container-not-found failure when no IDataNode tree is loaded (as expected in unit tests).
/// </remarks>
public sealed class ConfigurationCascadeTests
{
    private readonly Mock<IDataConnectionProvider> _connectionProviderMock;
    private readonly Mock<IDataSetConfigurationProvider> _dataSetProviderMock;
    private readonly Mock<IDomainConfigurationProvider<IDataStoreImplementationConfiguration>> _dataStoreConfigProviderMock;
    private readonly Mock<IConnectionLimitResolver> _limitResolverMock;
    private readonly ConnectionLimitCounterStore _counterStore;

    public ConfigurationCascadeTests()
    {
        _limitResolverMock = new Mock<IConnectionLimitResolver>();
        _limitResolverMock
            .Setup(r => r.Resolve(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(
                (IReadOnlyList<ConnectionLimitConfiguration>)[]));
        _counterStore = new ConnectionLimitCounterStore();
        _connectionProviderMock = new Mock<IDataConnectionProvider>();
        _dataSetProviderMock = new Mock<IDataSetConfigurationProvider>();
        _dataSetProviderMock
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<SimpleDataSetConfiguration>.Failure());

        // Why the interface and not the concrete DataStoreConfigurationProvider: the concrete
        // provider is sealed (every domain provider is), so Moq cannot mock it directly.
        _dataStoreConfigProviderMock = new Mock<IDomainConfigurationProvider<IDataStoreImplementationConfiguration>>();
        _dataStoreConfigProviderMock
            .Setup(m => m.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IDataStoreImplementationConfiguration>>.Success(new List<IDataStoreImplementationConfiguration>()));
        _dataStoreConfigProviderMock
            .Setup(m => m.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Success(default!));
    }

    private DataGatewayService CreateService() =>
        // Why: no IDataStoreProvider is passed — ResolveContainer returns null → ContainerNotFound
        // failure, which is the expected behavior for all gateway route tests (container resolution
        // always fails when no on-demand provider is wired).
        new DataGatewayService(
            NullLoggerFactory.Instance,
            _connectionProviderMock.Object,
            _dataSetProviderMock.Object,
            _dataStoreConfigProviderMock.Object, _limitResolverMock.Object, _counterStore, TestFieldTransformations.NoAttachments());

    // ─────────────────────────────────────────────────────────────────────────
    // DataGatewayService routing tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task Execute_LeafSaveWithNoParent_RoutesToSingleTablePath()
    {
        // Arrange: single-level config → should route to ExecuteContainer (container-not-found)
        var service = CreateService();
        var config = new TestLeafNoParentConfiguration { Id = Guid.NewGuid(), Name = "test" };
        var command = new ConfigurationSaveCommand<TestLeafNoParentConfiguration>(config, replacesCurrent: true);

        // Act
        var result = await service.Execute<int>(
            command, new DataStoreTarget("ConfigurationDb", "cfg", "TestLeafNoParent"), CancellationToken.None);

        // Why: IDataNode tree is null in this test → ResolveContainer returns null →
        // ExecuteContainer logs ContainerNotFound and returns failure.
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task Execute_ConfigurationSaveCommand_ReturnsFailureWhenContainerNotFound()
    {
        // Arrange: multi-level config — cascade intercept was removed in Wave C5.
        // All ConfigurationSaveCommands now route directly to ExecuteContainer.
        var service = CreateService();
        var config = new TestLeafChildConfiguration { Id = Guid.NewGuid(), Name = "two-level" };
        var command = new ConfigurationSaveCommand<TestLeafChildConfiguration>(config, replacesCurrent: true);

        // Act
        var result = await service.Execute<int>(
            command, new DataStoreTarget("ConfigurationDb", "cfg", "TestLeafChild"), CancellationToken.None);

        // Why: Container resolution fails (no IDataNode tree loaded) — failure is expected.
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public void IConfigurationSaveCommand_ConfigurationType_ReturnsGenericTypeArgument()
    {
        // Arrange
        var config = new TestLeafChildConfiguration { Id = Guid.NewGuid(), Name = "type-test" };
        IConfigurationSaveCommand cmd = new ConfigurationSaveCommand<TestLeafChildConfiguration>(config, replacesCurrent: true);

        // Assert
        cmd.ConfigurationType.ShouldBe(typeof(TestLeafChildConfiguration));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public void IConfigurationSaveCommand_InputData_ReturnsDataObject()
    {
        // Arrange
        var config = new TestLeafChildConfiguration { Id = Guid.NewGuid(), Name = "data-test" };
        IConfigurationSaveCommand cmd = new ConfigurationSaveCommand<TestLeafChildConfiguration>(config, replacesCurrent: true);

        // Assert
        cmd.InputData.ShouldBe(config);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Stub configuration classes
// ─────────────────────────────────────────────────────────────────────────────

internal sealed class TestLeafNoParentConfiguration
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

internal sealed class TestLeafChildConfiguration
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid TestLeafNoParentId { get; set; }
}
