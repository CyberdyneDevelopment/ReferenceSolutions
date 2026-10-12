using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Verifies how <see cref="ConfigurationGatewayDataStoreProvider"/> uses <see cref="IDerivedConfigurationCache"/>
/// to keep a built store between commands (FDW-992).
/// </summary>
[Collection(nameof(DataServiceTestCollection))]
public sealed class ConfigurationGatewayDataStoreProviderDerivedTests
{
    private readonly Mock<IDomainConfigurationProvider<IDataStoreImplementationConfiguration>> _config = new();
    private readonly Mock<IDataStoreBuilderSelector> _selector = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStoreNotInTheSchemaIsDerivedThroughAGatewayThatOffersTheCache()
    {
        var store = StoreWithContainers(1);
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.DataStores).Returns([]);
        var derived = gateway.As<IDerivedConfigurationCache>();
        derived
            .Setup(d => d.Derive<IDataStore>(
                "AuthDb",
                It.IsAny<Func<Task<IGenericResult<IDataStore>>>>(),
                It.IsAny<Func<IDataStore, bool>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStore>.Success(store));
        var provider = ProviderOver(gateway.Object);

        var result = await provider.Get("AuthDb", TestContext.Current.CancellationToken);

        result.Value.ShouldBeSameAs(store);
        derived.Verify(
            d => d.Derive<IDataStore>(
                "AuthDb",
                It.IsAny<Func<Task<IGenericResult<IDataStore>>>>(),
                It.IsAny<Func<IDataStore, bool>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheBuildTheCacheIsGivenIsTheCoreProvidersBuild()
    {
        var builds = new List<Func<Task<IGenericResult<IDataStore>>>>();
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.DataStores).Returns([]);
        gateway.As<IDerivedConfigurationCache>()
            .Setup(d => d.Derive<IDataStore>(
                "AuthDb",
                It.IsAny<Func<Task<IGenericResult<IDataStore>>>>(),
                It.IsAny<Func<IDataStore, bool>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Func<Task<IGenericResult<IDataStore>>>, Func<IDataStore, bool>, CancellationToken>(
                (_, build, _, _) => builds.Add(build))
            .ReturnsAsync(GenericResult<IDataStore>.Success(StoreWithContainers(1)));
        _config
            .Setup(p => p.Get("AuthDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Failure(new GenericMessage("not built in this test")));
        var provider = ProviderOver(gateway.Object);

        _ = await provider.Get("AuthDb", TestContext.Current.CancellationToken);
        var built = await builds.Single()();

        built.IsSuccess.ShouldBeFalse();
        _config.Verify(p => p.Get("AuthDb", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task OnlyAStoreWithContainersIsWorthKeeping()
    {
        var predicates = new List<Func<IDataStore, bool>>();
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.DataStores).Returns([]);
        gateway.As<IDerivedConfigurationCache>()
            .Setup(d => d.Derive<IDataStore>(
                "AuthDb",
                It.IsAny<Func<Task<IGenericResult<IDataStore>>>>(),
                It.IsAny<Func<IDataStore, bool>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Func<Task<IGenericResult<IDataStore>>>, Func<IDataStore, bool>, CancellationToken>(
                (_, _, keep, _) => predicates.Add(keep))
            .ReturnsAsync(GenericResult<IDataStore>.Success(StoreWithContainers(1)));
        var provider = ProviderOver(gateway.Object);

        _ = await provider.Get("AuthDb", TestContext.Current.CancellationToken);
        var isWorthKeeping = predicates.Single();

        // A store built while the caller could see none of its configuration has no containers.
        isWorthKeeping(StoreWithContainers(0)).ShouldBeFalse();
        isWorthKeeping(StoreWithContainers(1)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AGatewayThatOffersNoDerivedCacheBuildsTheStoreAsBefore()
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.DataStores).Returns([]);
        _config
            .Setup(p => p.Get("AuthDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Failure(new GenericMessage("not found")));
        var provider = ProviderOver(gateway.Object);

        var result = await provider.Get("AuthDb", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _config.Verify(p => p.Get("AuthDb", It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private ConfigurationGatewayDataStoreProvider ProviderOver(IConfigurationGateway gateway)
        => new(
            NullLogger<ConfigurationGatewayDataStoreProvider>.Instance,
            new ConfiguredDataStoreProvider(NullLogger<ConfiguredDataStoreProvider>.Instance, _config.Object, _selector.Object),
            _config.Object,
            new AnyConnectionGateways(gateway));

    private static IDataStore StoreWithContainers(int containers)
    {
        var path = new Mock<IDataNodePath>();
        path.Setup(p => p.Containers).Returns(Enumerable.Range(0, containers).Select(_ => Mock.Of<IDataContainer>()).ToList());

        var store = new Mock<IDataStore>();
        store.Setup(s => s.Paths).Returns(new List<IDataNodePath> { path.Object });
        return store.Object;
    }

    private sealed class AnyConnectionGateways : IConfigurationGatewayProvider
    {
        private readonly IConfigurationGateway _gateway;

        public AnyConnectionGateways(IConfigurationGateway gateway) => _gateway = gateway;

        public IGenericResult<IConfigurationGateway> Get(string connectionName)
            => GenericResult<IConfigurationGateway>.Success(_gateway);

        public IGenericResult Register(IConfigurationGateway gateway) => GenericResult.Success();
    }
}
