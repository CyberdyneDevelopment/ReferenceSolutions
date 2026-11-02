using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
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

// FDW-1354: a vectorized command runs only on a store whose connection is vectorized, under that connection's limits.
[Collection(nameof(DataServiceTestCollection))]
public sealed class DataGatewayVectorizedTests
{
    private const string Store = "TechComDb";
    private const string Passages = "ConversationPassage";

    private readonly Mock<IDataConnectionProvider> _connections = new();
    private readonly Mock<IDataSetConfigurationProvider> _dataSets = new();
    private readonly Mock<IDomainConfigurationProvider<IDataStoreImplementationConfiguration>> _storeConfigs = new();
    private readonly Mock<IDataStoreProvider> _stores = new();
    private readonly Mock<IConnectionLimitResolver> _limits = new();
    private readonly Guid _connectionId = Guid.NewGuid();
    private readonly Mock<IDataContainer> _container = new();

    public DataGatewayVectorizedTests()
    {
        _limits.Setup(r => r.Resolve(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success((IReadOnlyList<ConnectionLimitConfiguration>)[]));
        _dataSets.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<SimpleDataSetConfiguration>.Failure());

        _container.Setup(c => c.Name).Returns(Passages);
        _stores.Setup(p => p.Get(Store, It.IsAny<string>(), Passages, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Success(_container.Object));
        _storeConfigs.Setup(m => m.Get(Store, ConfigurationLevels.RecordOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Success(
                new DataStoreImplementationConfiguration { Name = Store, ConnectionId = _connectionId }));
    }

    private DataGatewayService Gateway() => new(
        NullLoggerFactory.Instance, _connections.Object, _dataSets.Object, _storeConfigs.Object, _limits.Object,
        new ConnectionLimitCounterStore(), TestFieldTransformations.NoAttachments(), dataStoreProvider: _stores.Object);

    private void ConnectionIs(IDataConnection connection) =>
        _connections.Setup(p => p.Get(_connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(connection));

    private static NearestCommand<object> Nearest(int take) => new()
    {
        VectorField = "Vector",
        Vector = [1f, 0f, 0f],
        Metric = VectorMetrics.Cosine,
        Take = take,
    };

    private static DataStoreTarget Target() => new(Store, "tc", Passages);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RefusesAStoreWhoseConnectionIsNotVectorized()
    {
        ConnectionIs(new Mock<IDataConnection>().Object);

        var result = await Gateway().Execute(Nearest(5), Target(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("does not hold vectors");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RunsOnAVectorizedConnectionAgainstTheTargetsContainer()
    {
        IReadOnlyList<NearestResult<object>> nearest = [new NearestResult<object> { Record = "passage", Distance = 0.25 }];
        var connection = new Mock<IVectorizedDataConnection>();
        connection
            .Setup(c => c.Execute(It.IsAny<IVectorizedDataCommand<IReadOnlyList<NearestResult<object>>>>(), _container.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<NearestResult<object>>>.Success(nearest));
        ConnectionIs(connection.Object);

        var result = await Gateway().Execute(Nearest(5), Target(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBeSameAs(nearest);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task HoldsANearestSearchToTheConnectionsMaxRows()
    {
        _limits.Setup(r => r.Resolve(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(
                (IReadOnlyList<ConnectionLimitConfiguration>)[new TestConnectionLimit(_connectionId, maxRows: 5)]));
        var connection = new Mock<IVectorizedDataConnection>();
        ConnectionIs(connection.Object);

        var result = await Gateway().Execute(Nearest(10), Target(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        connection.Verify(
            c => c.Execute(It.IsAny<IVectorizedDataCommand<IReadOnlyList<NearestResult<object>>>>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
