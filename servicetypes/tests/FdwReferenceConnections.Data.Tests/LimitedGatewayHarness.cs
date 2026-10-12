using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Limits;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A <see cref="DataGatewayService"/> wired to one DataStore, one container and one connection that can
/// both execute a command and open a cursor, with the connection's limits supplied by the test.
/// </summary>
internal sealed class LimitedGatewayHarness
{
    private const string Store = "limited-store";
    private const string ContainerName = "Events";

    private readonly Mock<IDataConnectionProvider> _connectionProvider = new();
    private readonly Mock<IDataSetConfigurationProvider> _dataSetProvider = new();
    private readonly Mock<IDomainConfigurationProvider<IDataStoreImplementationConfiguration>> _dataStoreConfigProvider = new();
    private readonly Mock<IDataStoreProvider> _dataStoreProvider = new();
    private readonly Mock<IConnectionLimitResolver> _limitResolver = new();

    public LimitedGatewayHarness()
    {
        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns(ContainerName);
        container.Setup(c => c.Keys).Returns((IReadOnlyList<IContainerKey>)Array.Empty<IContainerKey>());
        container.Setup(c => c.Nodes).Returns((IReadOnlyList<IDataNode>)Array.Empty<IDataNode>());
        container.Setup(c => c.Description).Returns((string?)null);
        _dataStoreProvider
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Success(container.Object));

        // The gateway reads the store's row alone, for its ConnectionId, on both paths (FDW-970).
        _dataStoreConfigProvider
            .Setup(p => p.Get(Store, ConfigurationLevels.RecordOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Success(
                new DataStoreImplementationConfiguration { Name = Store, ConnectionId = ConnectionId }));

        _dataSetProvider
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<SimpleDataSetConfiguration>.Failure());

        Connection = new Mock<IDataConnection>();
        Cursor = Connection.As<IRecordSourceConnection>();
        _connectionProvider
            .Setup(p => p.Get(ConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(Connection.Object));
    }

    public Guid ConnectionId { get; } = Guid.NewGuid();

    public ConnectionLimitCounterStore Counters { get; } = new();

    public Mock<IDataConnection> Connection { get; }

    public Mock<IRecordSourceConnection> Cursor { get; }

    public DataStoreTarget Target => new(Store, null, ContainerName);

    public DataGatewayService Gateway(params ConnectionLimitConfiguration[] limits)
    {
        _limitResolver
            .Setup(r => r.Resolve(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(limits.ToList()));
        return Build();
    }

    public DataGatewayService GatewayWhoseLimitsCannotBeResolved()
    {
        _limitResolver
            .Setup(r => r.Resolve(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Failure(new GenericMessage("limits unavailable")));
        return Build();
    }

    private DataGatewayService Build()
        => new(
            NullLoggerFactory.Instance,
            _connectionProvider.Object,
            _dataSetProvider.Object,
            _dataStoreConfigProvider.Object,
            _limitResolver.Object,
            Counters,
            TestFieldTransformations.NoAttachments(),
            dataStoreProvider: _dataStoreProvider.Object);
}
