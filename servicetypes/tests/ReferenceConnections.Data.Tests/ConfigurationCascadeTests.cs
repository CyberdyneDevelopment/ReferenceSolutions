using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

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
    private readonly Mock<DataStoreConfigurationProvider> _dataStoreConfigProviderMock;

    public ConfigurationCascadeTests()
    {
        _connectionProviderMock = new Mock<IDataConnectionProvider>();
        _dataSetProviderMock = new Mock<IDataSetConfigurationProvider>();
        _dataSetProviderMock
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataSetConfiguration>.Failure());

        // Why: Castle DynamicProxy requires all constructor args to be explicit, including optional ones.
        // The nullable cast satisfies the ICacheInvalidator? optional param.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
        _dataStoreConfigProviderMock = new Mock<DataStoreConfigurationProvider>(
            NullLogger<DataStoreConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => null!),
            new Lazy<Fdw.Services.Configuration.DefaultConfigurationProvider<Fdw.Services.Connections.DataContainerConfiguration, Fdw.Services.Connections.Commands.DataContainerConfigurationCommand>>(() => null!),
            "ConfigurationDb",
            "data",
            (ICacheInvalidator?)null) { CallBase = false };
#pragma warning restore CS8625
        _dataStoreConfigProviderMock
            .Setup(m => m.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataStoreConfiguration>>.Success(new List<DataStoreConfiguration>()));
#pragma warning disable CS8620
        _dataStoreConfigProviderMock
            .Setup(m => m.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration?>.Success(null));
#pragma warning restore CS8620
    }

    private DataGatewayService CreateService() =>
        // Why: no IDataStoreProvider is passed — ResolveContainer returns null → ContainerNotFound
        // failure, which is the expected behavior for all gateway route tests (container resolution
        // always fails when no on-demand provider is wired).
        new DataGatewayService(
            NullLoggerFactory.Instance,
            _connectionProviderMock.Object,
            new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object),
            _dataStoreConfigProviderMock.Object);

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
        var command = new ConfigurationSaveCommand<TestLeafNoParentConfiguration>(config);

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
        var command = new ConfigurationSaveCommand<TestLeafChildConfiguration>(config);

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
        IConfigurationSaveCommand cmd = new ConfigurationSaveCommand<TestLeafChildConfiguration>(config);

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
        IConfigurationSaveCommand cmd = new ConfigurationSaveCommand<TestLeafChildConfiguration>(config);

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
