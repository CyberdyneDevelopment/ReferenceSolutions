using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Limits;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-1068: the stores the host declares in configurationSchema.json (PlatformConfiguration, ServerConfiguration,
/// AuthDb) hold the platform's own configuration, credentials and sessions. The main data gateway refuses a request
/// over one -- whether it names the store and container directly, as /find and /schema/preview do, a container with
/// no path, as the quality checks do, or a data set over it -- unless the caller is the system context, which is sent
/// to the store's configuration gateway. Which stores those are is a fact the store carries (DeclaredByHost), never a
/// list of names.
/// </summary>
[Collection(nameof(DataServiceTestCollection))]
public sealed class HostDeclaredStoreGatewayTests
{
    private readonly Mock<IDataConnectionProvider> _connections = new();
    private readonly Mock<IDataSetConfigurationProvider> _dataSets = new();
    private readonly Mock<IDomainConfigurationProvider<IDataStoreImplementationConfiguration>> _storeRows = new();
    private readonly Mock<IDataStoreProvider> _stores = new();
    private readonly Mock<IConnectionLimitResolver> _limits = new();
    private readonly Mock<IConfigurationGatewayProvider> _configurationGateways = new();
    private readonly Mock<IConfigurationGateway> _platformGateway = new();

    public HostDeclaredStoreGatewayTests()
    {
        _limits.Setup(r => r.Resolve(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success((IReadOnlyList<ConnectionLimitConfiguration>)[]));
        _stores.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Failure(new GenericMessage("container not found")));
        Store("PlatformConfiguration", declaredByHost: true);
        Store("AuthDb", declaredByHost: true);
        Store("NflDb", declaredByHost: false);
        _configurationGateways.Setup(p => p.Get("PlatformConfiguration"))
            .Returns(GenericResult<IConfigurationGateway>.Success(_platformGateway.Object));
        _platformGateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success([]));
    }

    private void Store(string name, bool declaredByHost)
    {
        var store = new Mock<IDataStore>();
        store.SetupGet(s => s.Name).Returns(name);
        store.SetupGet(s => s.DeclaredByHost).Returns(declaredByHost);
        _stores.Setup(p => p.Get(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStore>.Success(store.Object));
    }

    private static IAuthenticationContextAccessor Caller(bool system)
    {
        var context = new Mock<IAuthenticationContext>();
        context.SetupGet(c => c.IsSystemContext).Returns(system);
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(context.Object);
        return accessor.Object;
    }

    private DataGatewayService Gateway(bool system)
        => new(NullLoggerFactory.Instance, _connections.Object, _dataSets.Object, _storeRows.Object, _limits.Object,
            new ConnectionLimitCounterStore(), TestFieldTransformations.NoAttachments(),
            dataStoreProvider: _stores.Object,
            authenticationContextAccessor: Caller(system),
            configurationGateways: _configurationGateways.Object);

    private static IDataCommand Query() => new Mock<IQueryCommand>().Object;

    private static IResultCode Refused => DataServiceResultCodes.ByName("HostDeclaredStoreRefused");

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("PlatformConfiguration", "sec", "Secret")]   // what /find and /schema/preview send
    [InlineData("PlatformConfiguration", null, "DataSet")]   // what a quality check sends: no path
    [InlineData("AuthDb", "auth", "Session")]
    public async Task ARequestOverAHostStoreIsRefusedToAUser(string store, string? path, string container)
    {
        var result = await Gateway(system: false).Execute<IEnumerable<Dictionary<string, object?>>>(
            Query(), new DataStoreTarget(store, path, container), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeSameAs(Refused);
        _platformGateway.VerifyNoOtherCalls();
        _connections.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheSystemContextReadsAHostStoreThroughItsConfigurationGateway()
    {
        var target = new DataStoreTarget("PlatformConfiguration", "sec", "Secret");

        var result = await Gateway(system: true).Execute<IEnumerable<Dictionary<string, object?>>>(
            Query(), target, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _platformGateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), target, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task OpeningARecordSourceOverAHostStoreIsRefusedToAUser()
    {
        var result = await Gateway(system: false).OpenRecordSource(
            Query(), new DataStoreTarget("PlatformConfiguration", "usr", "Users"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeSameAs(Refused);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADataSetOverAHostStoreIsRefusedToAUser()
    {
        var dataSet = new SimpleDataSetConfiguration
        {
            Name = "Secrets",
            Implementation = "Simple",
            Sources = [new DataSetSourceConfiguration { SourceName = "s", DataStoreName = "PlatformConfiguration", ContainerName = "Secret" }],
        };
        _dataSets.Setup(p => p.Get("Secrets", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(dataSet));

        var result = await Gateway(system: false).Execute<IEnumerable<Dictionary<string, object?>>>(
            Query(), new DataSetTarget("Secrets"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeSameAs(Refused);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AStoreConfiguredAtRunTimeIsNotRefused()
    {
        var result = await Gateway(system: false).Execute<IEnumerable<Dictionary<string, object?>>>(
            Query(), new DataStoreTarget("NflDb", "NflData", "Team"), TestContext.Current.CancellationToken);

        result.Code.ShouldNotBeSameAs(Refused);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void TheRefusalIsAValidationCodeWhoseReasonNamesTheStore()
    {
        Refused.Id.ShouldBe(20007);
        Refused.FormatClientMessage(ResultDetails.Create("dataStore", "PlatformConfiguration", "container", "Secret"))
            .ShouldBe("Data store 'PlatformConfiguration' is the platform's own and is not reachable from a request");
    }
}
