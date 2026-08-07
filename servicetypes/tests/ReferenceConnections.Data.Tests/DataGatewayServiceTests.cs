using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Data.Tests;

[Collection(nameof(DataServiceTestCollection))]
public sealed class DataGatewayServiceTests
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Mock<IDataConnectionProvider> _connectionProviderMock;
    private readonly Mock<IDataSetConfigurationProvider> _dataSetProviderMock;
    private readonly Mock<DataStoreConfigurationProvider> _dataStoreConfigProviderMock;
    // Why: container routing now resolves the unified container on demand through IDataStoreProvider
    // (the eager full-tree singleton is gone). The interface is mockable; the concrete provider is sealed.
    private readonly Mock<IDataStoreProvider> _dataStoreProviderMock;

    public DataGatewayServiceTests()
    {
        _loggerFactory = NullLoggerFactory.Instance;
        _connectionProviderMock = new Mock<IDataConnectionProvider>();
        _dataSetProviderMock = new Mock<IDataSetConfigurationProvider>();
        // Why: DataGatewayService probes the DataSet provider for every container execute call.
        // Without a default setup, Moq returns null Task, causing NullReferenceException at the await site.
        _dataSetProviderMock.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataSetConfiguration>.Failure());

        // Why: DataStoreConfigurationProvider requires IOptionsMonitor, ILogger, and Lazy<IDataGateway> in constructor.
        // Why: Castle DynamicProxy requires all constructor args to be explicit — optional params are not inferred.
        _dataStoreConfigProviderMock = new Mock<DataStoreConfigurationProvider>(
            NullLogger<DataStoreConfigurationProvider>.Instance,
            new Lazy<Fdw.Services.Data.Abstractions.IConfigurationGateway>(() => null!),
            new Lazy<Fdw.Services.Configuration.DefaultConfigurationProvider<Fdw.Services.Connections.DataContainerConfiguration, Fdw.Services.Connections.Commands.DataContainerConfigurationCommand>>(() => null!),
            "ConfigurationDb",
            "data",
            null!) { CallBase = false };
        _dataStoreConfigProviderMock.Setup(m => m.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataStoreConfiguration>>.Success(new List<DataStoreConfiguration>()));
#pragma warning disable CS8620 // Why: Mock returns non-nullable generic result; nullable wrapping is intentional for "not found" test setup
        _dataStoreConfigProviderMock.Setup(m => m.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration?>.Success(null));
#pragma warning restore CS8620

        _dataStoreProviderMock = new Mock<IDataStoreProvider>();
        // Why: default to a clean "not found" failure (never a null Task) so unconfigured container
        // routing fails loud instead of throwing an NRE at the await site. Container resolution now
        // goes through the dot-walk Get(store, path, container) overload — there is no GetContainer verb.
        _dataStoreProviderMock
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Failure(new GenericMessage("container not found")));
    }

    // Why: Wires the on-demand routing path — IDataStoreProvider.Get(store, path, container) resolves
    // the unified container under storeName/pathName, and the config provider returns a DataStore
    // config carrying the ConnectionId. Returns the connectionId so tests can set up
    // _connectionProviderMock.Get(connectionId, ct).
    private Guid BuildDataStoreTree(
        string storeName,
        string containerName,
        string pathName = "dbo")
    {
        var mockContainer = new Mock<IDataContainer>();
        mockContainer.Setup(c => c.Name).Returns(containerName);
        // Why: Use empty arrays cast to IReadOnlyList<T> so Moq resolves the correct overload.
        mockContainer.Setup(c => c.Keys).Returns((IReadOnlyList<IContainerKey>)Array.Empty<IContainerKey>());
        mockContainer.Setup(c => c.Nodes).Returns((IReadOnlyList<IDataNode>)Array.Empty<IDataNode>());
        mockContainer.Setup(c => c.Description).Returns((string?)null);

        var connectionId = Guid.NewGuid();

        // Why: container resolution goes through the dot-walk Get(store, path, container) overload.
        // The real provider matches store and container names case-insensitively — mirror that here so
        // the case-insensitive lookup test resolves.
        _dataStoreProviderMock
            .Setup(p => p.Get(
                It.Is<string>(n => string.Equals(n, storeName, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<string>(),
                It.Is<string>(n => string.Equals(n, containerName, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Success(mockContainer.Object));
        _dataStoreProviderMock
            .Setup(p => p.Get(
                It.Is<string>(n => !string.Equals(n, storeName, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Failure(new GenericMessage("container not found")));
        _dataStoreProviderMock
            .Setup(p => p.Get(
                It.IsAny<string>(), It.IsAny<string>(),
                It.Is<string>(n => !string.Equals(n, containerName, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Failure(new GenericMessage("container not found")));

        // Why: the connection is resolved from the DataStore config's ConnectionId (read on demand).
        var storeConfig = new DataStoreConfiguration { Name = storeName, ConnectionId = connectionId };
        _dataStoreConfigProviderMock
            .Setup(m => m.Get(storeName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Success(storeConfig));

        return connectionId;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_ShouldInitializeService()
    {
        // Act
        var service = new DataGatewayService(_loggerFactory, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);

        // Assert
        service.ShouldNotBeNull();
        service.ShouldBeAssignableTo<IDataGateway>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_ShouldReturnFailure_WhenContainerNotFound()
    {
        // Arrange — null dataStores means ResolveContainer returns null (container not found)
        var service = new DataGatewayService(_loggerFactory, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);
        var commandMock = new Mock<IDataCommand>();
        commandMock.Setup(c => c.CommandType).Returns("Query");
        // Why: Addressing lives in DataStoreTarget now — not on the command.
        var target = new DataStoreTarget("test-connection", null, "NonExistentContainer");

        // Act
        var result = await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_ShouldReturnFailure_WhenConnectionNotFound()
    {
        // Arrange — container tree resolves "TestContainer" under "test-connection"
        var connectionId = BuildDataStoreTree("test-connection", "TestContainer");

        var service = new DataGatewayService(_loggerFactory, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);
        var commandMock = new Mock<IDataCommand>();
        commandMock.Setup(c => c.CommandType).Returns("Query");
        var target = new DataStoreTarget("test-connection", null, "TestContainer");

        // Why: Connection is resolved from DataStore.ConnectionId — set up Get(Guid, ct).
        _connectionProviderMock
            .Setup(p => p.Get(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Failure(new GenericMessage("Connection not found")));

        // Act
        var result = await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_ShouldReturnFailure_WhenConnectionResultValueIsNull()
    {
        // Arrange
        var connectionId = BuildDataStoreTree("test-connection", "TestContainer");

        var service = new DataGatewayService(_loggerFactory, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);
        var commandMock = new Mock<IDataCommand>();
        commandMock.Setup(c => c.CommandType).Returns("Query");
        var target = new DataStoreTarget("test-connection", null, "TestContainer");

        // Why: Connection is resolved from DataStore.ConnectionId — set up Get(Guid, ct).
        _connectionProviderMock
            .Setup(p => p.Get(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(null!));

        // Act
        var result = await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_ShouldCallConnectionExecute_WhenConnectionFound()
    {
        // Arrange
        var connectionId = BuildDataStoreTree("test-connection", "TestContainer");

        var service = new DataGatewayService(_loggerFactory, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);
        var commandMock = new Mock<IDataCommand>();
        commandMock.Setup(c => c.CommandType).Returns("Query");
        var target = new DataStoreTarget("test-connection", null, "TestContainer");

        var connectionMock = new Mock<IDataConnection>();
        var expectedResult = GenericResult<string>.Success("test-result");
        connectionMock
            .Setup(c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Why: Connection is resolved from DataStore.ConnectionId — set up Get(Guid, ct).
        _connectionProviderMock
            .Setup(p => p.Get(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(connectionMock.Object));

        // Act
        var result = await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("test-result");
        connectionMock.Verify(c => c.Execute<string>(commandMock.Object, It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_ShouldPassCancellationToken_ToConnection()
    {
        // Arrange
        var connectionId = BuildDataStoreTree("test-connection", "TestContainer");

        var service = new DataGatewayService(_loggerFactory, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);
        var commandMock = new Mock<IDataCommand>();
        commandMock.Setup(c => c.CommandType).Returns("Query");
        var target = new DataStoreTarget("test-connection", null, "TestContainer");

        var connectionMock = new Mock<IDataConnection>();
        var cancellationToken = new CancellationToken();
        var expectedResult = GenericResult<int>.Success(42);
        connectionMock
            .Setup(c => c.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), cancellationToken))
            .ReturnsAsync(expectedResult);

        // Why: Connection is resolved from DataStore.ConnectionId — set up Get(Guid, ct).
        _connectionProviderMock
            .Setup(p => p.Get(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(connectionMock.Object));

        // Act
        var result = await service.Execute<int>(commandMock.Object, target, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        connectionMock.Verify(c => c.Execute<int>(commandMock.Object, It.IsAny<IDataContainer>(), cancellationToken), Times.Once);
    }

    // NOTE: Logging tests removed - LoggerMessage source generators don't go through ILogger.Log
    // Testing logging is an implementation detail and these tests were incorrectly verifying
    // the base Log method instead of the generated extension methods

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_ShouldExecuteCommand_WhenRoutingCommand()
    {
        // Arrange
        var connectionId = BuildDataStoreTree("test-connection", "TestContainer");

        var service = new DataGatewayService(_loggerFactory, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);
        var commandMock = new Mock<IDataCommand>();
        commandMock.Setup(c => c.CommandType).Returns("Query");
        var target = new DataStoreTarget("test-connection", null, "TestContainer");

        var connectionMock = new Mock<IDataConnection>();
        connectionMock
            .Setup(c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("result"));

        // Why: Connection is resolved from DataStore.ConnectionId — set up Get(Guid, ct).
        _connectionProviderMock
            .Setup(p => p.Get(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(connectionMock.Object));

        // Act
        var result = await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("result");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_ShouldPassContainerToConnection()
    {
        // Arrange
        var connectionId = BuildDataStoreTree("test-connection", "TestContainer");

        var service = new DataGatewayService(_loggerFactory, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);
        var commandMock = new Mock<IDataCommand>();
        commandMock.Setup(c => c.CommandType).Returns("Query");
        var target = new DataStoreTarget("test-connection", null, "TestContainer");

        var connectionMock = new Mock<IDataConnection>();
        // Why: Verify a non-null IDataContainer is passed to the connection.
        connectionMock
            .Setup(c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("result"));

        // Why: Connection is resolved from DataStore.ConnectionId — set up Get(Guid, ct).
        _connectionProviderMock
            .Setup(p => p.Get(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(connectionMock.Object));

        // Act
        await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert — verify that a non-null IDataContainer was passed to the connection
        connectionMock.Verify(c => c.Execute<string>(
            It.IsAny<IDataCommand>(),
            It.Is<IDataContainer>(dc => dc != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_ShouldUseCaseInsensitiveContainerLookup()
    {
        // Arrange — container registered as "TestContainer", target uses lowercase "testcontainer"
        // Why: ResolveContainer uses StringComparison.OrdinalIgnoreCase in the IDataNode tree scan.
        var connectionId = BuildDataStoreTree("test-connection", "TestContainer");

        var service = new DataGatewayService(_loggerFactory, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);
        var commandMock = new Mock<IDataCommand>();
        commandMock.Setup(c => c.CommandType).Returns("Query");
        // Why: Different case in target.Container — tests case-insensitive resolution.
        var target = new DataStoreTarget("test-connection", null, "testcontainer");

        var connectionMock = new Mock<IDataConnection>();
        connectionMock
            .Setup(c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("result"));

        // Why: Connection is resolved from DataStore.ConnectionId — set up Get(Guid, ct).
        _connectionProviderMock
            .Setup(p => p.Get(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(connectionMock.Object));

        // Act
        var result = await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_ShouldReturnGenericResult_WithCorrectType()
    {
        // Arrange
        var connectionId = BuildDataStoreTree("test-connection", "TestContainer");

        var service = new DataGatewayService(_loggerFactory, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);
        var commandMock = new Mock<IDataCommand>();
        commandMock.Setup(c => c.CommandType).Returns("Query");
        var target = new DataStoreTarget("test-connection", null, "TestContainer");

        var connectionMock = new Mock<IDataConnection>();
        connectionMock
            .Setup(c => c.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(42));

        // Why: Connection is resolved from DataStore.ConnectionId — set up Get(Guid, ct).
        _connectionProviderMock
            .Setup(p => p.Get(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(connectionMock.Object));

        // Act
        var result = await service.Execute<int>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeOfType<GenericResult<int>>();
        result.Value.ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_ShouldPropagateConnectionExecuteResult()
    {
        // Arrange
        var connectionId = BuildDataStoreTree("test-connection", "TestContainer");

        var service = new DataGatewayService(_loggerFactory, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);
        var commandMock = new Mock<IDataCommand>();
        commandMock.Setup(c => c.CommandType).Returns("Query");
        var target = new DataStoreTarget("test-connection", null, "TestContainer");

        var connectionMock = new Mock<IDataConnection>();
        var expectedResult = GenericResult<string>.Failure(new GenericMessage("Connection error"));
        connectionMock
            .Setup(c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Why: Connection is resolved from DataStore.ConnectionId — set up Get(Guid, ct).
        _connectionProviderMock
            .Setup(p => p.Get(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(connectionMock.Object));

        // Act
        var result = await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.Any(m => m.Message.Contains("Connection error")).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithNullLoggerFactoryUsesNullLogger()
    {
        // Act - null loggerFactory should not throw
        var service = new DataGatewayService(null, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);

        // Assert
        service.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteUsesDataStoreNameFromTarget()
    {
        // Arrange — container is registered under "my-datastore"
        // Why: Addressing comes exclusively from DataStoreTarget; the gateway resolves the
        // physical connection from DataStore.ConnectionId.
        var connectionId = BuildDataStoreTree("my-datastore", "TestContainer");

        var service = new DataGatewayService(_loggerFactory, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);
        var commandMock = new Mock<IDataCommand>();
        commandMock.Setup(c => c.CommandType).Returns("Query");
        var target = new DataStoreTarget("my-datastore", null, "TestContainer");

        var connectionMock = new Mock<IDataConnection>();
        connectionMock
            .Setup(c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("result"));

        // Why: Connection is resolved from DataStore.ConnectionId — set up Get(Guid, ct).
        _connectionProviderMock
            .Setup(p => p.Get(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(connectionMock.Object));

        // Act
        var result = await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteUsesPathFromTarget()
    {
        // Arrange — container resolved via path "dbo" in store "test-connection"
        // Why: When target.Path is provided, ResolveContainer uses store.Path(path).Container(container).
        var connectionId = BuildDataStoreTree("test-connection", "TestContainer", pathName: "dbo");

        var service = new DataGatewayService(_loggerFactory, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);
        var commandMock = new Mock<IDataCommand>();
        commandMock.Setup(c => c.CommandType).Returns("Query");
        // Why: Path is specified in the target — tests the path-specified resolution branch.
        var target = new DataStoreTarget("test-connection", "dbo", "TestContainer");

        var connectionMock = new Mock<IDataConnection>();
        connectionMock
            .Setup(c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("result"));

        // Why: Connection is resolved from DataStore.ConnectionId — set up Get(Guid, ct).
        _connectionProviderMock
            .Setup(p => p.Get(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(connectionMock.Object));

        // Act
        var result = await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteRoutesToDataSetWhenContainerNameIsRegisteredDataSet()
    {
        // Arrange
        var dataSet = new DataSetConfiguration { Name = "FederatedProducts" };
        dataSet.Sources = new List<DataSetSourceConfiguration>();
        dataSet.Fields = new List<DataFieldConfiguration>();
        dataSet.Joins = new List<JoinConfiguration>();

        _dataSetProviderMock
            .Setup(p => p.Get("FederatedProducts", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataSetConfiguration>.Success(dataSet));

        var service = new DataGatewayService(_loggerFactory, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);
        var commandMock = new Mock<IDataCommand>();
        commandMock.Setup(c => c.CommandType).Returns("Query");
        // Why: DataSet routing uses DataSetTarget; DataStoreTarget is for container routing.
        var dataSetTarget = new DataSetTarget("FederatedProducts");

        // Act - this will fail because no sources, but it proves the dataset path was taken
        var result = await service.Execute<string>(commandMock.Object, dataSetTarget, TestContext.Current.CancellationToken);

        // Assert — fails because DataSetTypes.ByName() (static TypeCollection) hasn't been
        // seeded with "FederatedProducts" in the test process; falls through to no-sources failure.
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteDataSetReturnsFailureWhenDataSetNotFound()
    {
        // Arrange — first Get() succeeds (probe for routing), second fails (execution)
        var dataSet = new DataSetConfiguration { Name = "MissingDataSet" };
        dataSet.Sources = new List<DataSetSourceConfiguration>();
        dataSet.Fields = new List<DataFieldConfiguration>();
        dataSet.Joins = new List<JoinConfiguration>();

        // Why: Execute() routes via IsDataSetRegistered; Get() is called once inside
        // ResolveDataSetAndSources. Return Failure directly to simulate dataset not found.
        _dataSetProviderMock
            .Setup(p => p.Get("MissingDataSet", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataSetConfiguration>.Failure());

        var service = new DataGatewayService(_loggerFactory, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);
        var commandMock = new Mock<IDataCommand>();
        commandMock.Setup(c => c.CommandType).Returns("Query");
        var dataSetTarget = new DataSetTarget("MissingDataSet");

        // Act
        var result = await service.Execute<string>(commandMock.Object, dataSetTarget, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteDataSetReturnsFailureWhenSourceResolutionReturnsEmpty()
    {
        // Arrange — DataSet exists but has no sources resolvable (empty source IDs)
        var dataSet = new DataSetConfiguration { Name = "BadDataSet" };
        dataSet.Sources = new List<DataSetSourceConfiguration>();

        _dataSetProviderMock
            .Setup(p => p.Get("BadDataSet", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataSetConfiguration>.Success(dataSet));

        var service = new DataGatewayService(_loggerFactory, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);
        var commandMock = new Mock<IDataCommand>();
        commandMock.Setup(c => c.CommandType).Returns("Query");
        var dataSetTarget = new DataSetTarget("BadDataSet");

        // Act
        var result = await service.Execute<string>(commandMock.Object, dataSetTarget, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteDataSetReturnsFailureWhenSourceHasNoConnection()
    {
        // Arrange — DataSet exists with a source, but the source has no ConnectionName configured;
        // execution fails when the DataSetType dispatch cannot resolve the connection.
        var dataSet = new DataSetConfiguration { Name = "UnresolvableDataSet" };
        dataSet.Sources = new List<DataSetSourceConfiguration> { new() { Id = Guid.NewGuid() } };

        _dataSetProviderMock
            .Setup(p => p.Get("UnresolvableDataSet", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataSetConfiguration>.Success(dataSet));

        var service = new DataGatewayService(_loggerFactory, _connectionProviderMock.Object, new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object), _dataStoreConfigProviderMock.Object, dataStoreProvider: _dataStoreProviderMock.Object);
        var commandMock = new Mock<IDataCommand>();
        commandMock.Setup(c => c.CommandType).Returns("Query");
        var dataSetTarget = new DataSetTarget("UnresolvableDataSet");

        // Act
        var result = await service.Execute<string>(commandMock.Object, dataSetTarget, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }
}
