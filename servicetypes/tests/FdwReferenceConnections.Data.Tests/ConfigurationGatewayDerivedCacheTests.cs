using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Caching;
using Fdw.Services.Data.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Verifies <see cref="ConfigurationGateway.Derive{T}"/>: a value built from configuration reads is kept
/// under the caller's partition and dropped by a write to a container the build read (FDW-992).
/// </summary>
/// <remarks>
/// Every read a build makes here is answered from a pre-seeded cache entry, so no connection is needed:
/// the gateway records the read on entry whether it is answered from the cache or executed.
/// </remarks>
[Collection(nameof(DataServiceTestCollection))]
public sealed class ConfigurationGatewayDerivedCacheTests
{
    private static readonly DataStoreTarget ConnectionTable = new("ConfigurationDb", "conn", "Connection");
    private static readonly DataStoreTarget StoreTable = new("ConfigurationDb", "data", "DataStore");
    private static readonly DataStoreTarget PathTable = new("ConfigurationDb", "data", "DataPath");

    // The partition is asked of the connection kind, not spelled out, for the reason given in
    // ConfigurationGatewayCachingTests: the kind composes it. No accessor means the null scope.
    private static readonly string NullScopePartition = ConnectionTypes.ByName("MsSql").CachePartition(null);

    private readonly DataGatewayResultCache _cache = new(new MemoryCache(new MemoryCacheOptions()), NullLoggerFactory.Instance);
    private readonly Mock<IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>> _connections = new();
    private readonly Mock<IQueryCommand> _read = QueryCommandMock.Read();
    private readonly ConfigurationSchema _schema = new()
    {
        Connections = { new MsSqlConnectionConfiguration { MaxSerialInsert = 1000, Name = "PlatformConfiguration", Implementation = "MsSql" } },
    };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task DerivedValueIsBuiltOnceForRepeatedCallers()
    {
        var gateway = GatewayFor();
        SeedRead(ConnectionTable, NullScopePartition);
        var builds = 0;

        async Task<IGenericResult<string>> Build()
        {
            builds++;
            _ = await Read(gateway, ConnectionTable);
            return GenericResult<string>.Success("store");
        }

        var first = await gateway.Derive<string>("store", Build, static _ => true, TestContext.Current.CancellationToken);
        var second = await gateway.Derive<string>("store", Build, static _ => true, TestContext.Current.CancellationToken);

        first.Value.ShouldBe("store");
        second.Value.ShouldBe("store");
        builds.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AWriteToAContainerTheBuildReadDropsTheDerivedValue()
    {
        var gateway = GatewayFor();
        SeedRead(ConnectionTable, NullScopePartition);
        var builds = 0;

        async Task<IGenericResult<string>> Build()
        {
            builds++;
            _ = await Read(gateway, ConnectionTable);
            return GenericResult<string>.Success("store");
        }

        _ = await gateway.Derive<string>("store", Build, static _ => true, TestContext.Current.CancellationToken);

        // What a provider save does after its commit; it also drops the seeded read, which is put back.
        gateway.InvalidateCachedResults(ConnectionTable);
        SeedRead(ConnectionTable, NullScopePartition);
        _ = await gateway.Derive<string>("store", Build, static _ => true, TestContext.Current.CancellationToken);

        builds.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AWriteToAnUnrelatedContainerKeepsTheDerivedValue()
    {
        var gateway = GatewayFor();
        SeedRead(ConnectionTable, NullScopePartition);
        var builds = 0;

        async Task<IGenericResult<string>> Build()
        {
            builds++;
            _ = await Read(gateway, ConnectionTable);
            return GenericResult<string>.Success("store");
        }

        _ = await gateway.Derive<string>("store", Build, static _ => true, TestContext.Current.CancellationToken);
        gateway.InvalidateCachedResults(PathTable);
        _ = await gateway.Derive<string>("store", Build, static _ => true, TestContext.Current.CancellationToken);

        builds.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ConcurrentBuildsKeepTheirOwnTagsAndEachIsDroppedOnlyByItsOwnReads()
    {
        var gateway = GatewayFor();
        SeedRead(ConnectionTable, NullScopePartition);
        SeedRead(StoreTable, NullScopePartition);
        SeedRead(PathTable, NullScopePartition);
        var pausedInA = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int aBuilds = 0, bBuilds = 0;

        // A reads the connection table, waits while B is built and kept, then reads the path table. Both
        // of A's reads must be A's, and B's read of the store table, made in between, must not be.
        async Task<IGenericResult<string>> BuildA()
        {
            aBuilds++;
            _ = await Read(gateway, ConnectionTable);
            await pausedInA.Task;
            _ = await Read(gateway, PathTable);
            return GenericResult<string>.Success("a");
        }

        async Task<IGenericResult<string>> BuildB()
        {
            bBuilds++;
            _ = await Read(gateway, StoreTable);
            return GenericResult<string>.Success("b");
        }

        var aInFlight = gateway.Derive<string>("a", BuildA, static _ => true, TestContext.Current.CancellationToken);
        _ = await gateway.Derive<string>("b", BuildB, static _ => true, TestContext.Current.CancellationToken);
        pausedInA.SetResult();
        _ = await aInFlight;

        // A write to B's container drops B only.
        gateway.InvalidateCachedResults(StoreTable);
        SeedRead(StoreTable, NullScopePartition);
        _ = await gateway.Derive<string>("a", BuildA, static _ => true, TestContext.Current.CancellationToken);
        _ = await gateway.Derive<string>("b", BuildB, static _ => true, TestContext.Current.CancellationToken);
        aBuilds.ShouldBe(1, "a write to a container only B read must not drop A");
        bBuilds.ShouldBe(2);

        // A write to the container A read after B finished drops A only: that read was not lost from A.
        gateway.InvalidateCachedResults(PathTable);
        SeedRead(PathTable, NullScopePartition);
        _ = await gateway.Derive<string>("a", BuildA, static _ => true, TestContext.Current.CancellationToken);
        _ = await gateway.Derive<string>("b", BuildB, static _ => true, TestContext.Current.CancellationToken);
        aBuilds.ShouldBe(2);
        bBuilds.ShouldBe(2, "a write to a container only A read must not drop B");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ABuildInsideABuildIsDroppedByTheReadsOfBothEvenWhenTheInnerOneWasKept()
    {
        var gateway = GatewayFor();
        SeedRead(ConnectionTable, NullScopePartition);
        SeedRead(StoreTable, NullScopePartition);
        int outerBuilds = 0, innerBuilds = 0;

        async Task<IGenericResult<string>> BuildInner()
        {
            innerBuilds++;
            _ = await Read(gateway, StoreTable);
            return GenericResult<string>.Success("inner");
        }

        async Task<IGenericResult<string>> BuildOuter()
        {
            outerBuilds++;
            _ = await Read(gateway, ConnectionTable);
            _ = await gateway.Derive<string>("inner", BuildInner, static _ => true, TestContext.Current.CancellationToken);
            return GenericResult<string>.Success("outer");
        }

        // The inner value is kept first, so building the outer one is handed it from the cache: the inner
        // build does not run, and its reads have to reach the outer one another way.
        _ = await gateway.Derive<string>("inner", BuildInner, static _ => true, TestContext.Current.CancellationToken);
        _ = await gateway.Derive<string>("outer", BuildOuter, static _ => true, TestContext.Current.CancellationToken);
        innerBuilds.ShouldBe(1);
        outerBuilds.ShouldBe(1);

        gateway.InvalidateCachedResults(StoreTable);
        SeedRead(StoreTable, NullScopePartition);
        _ = await gateway.Derive<string>("outer", BuildOuter, static _ => true, TestContext.Current.CancellationToken);

        outerBuilds.ShouldBe(2, "the outer value depends on what the inner one read, whether or not the inner one ran");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ABuildThatReadNothingIsNotKept()
    {
        var gateway = GatewayFor();
        var builds = 0;

        Task<IGenericResult<string>> Build()
        {
            builds++;
            return Task.FromResult<IGenericResult<string>>(GenericResult<string>.Success("store"));
        }

        _ = await gateway.Derive<string>("store", Build, static _ => true, TestContext.Current.CancellationToken);
        _ = await gateway.Derive<string>("store", Build, static _ => true, TestContext.Current.CancellationToken);

        builds.ShouldBe(2, "no write could ever drop it, so it must not be kept");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AValueThatIsNotWorthKeepingIsHandedBackButNotKept()
    {
        var gateway = GatewayFor();
        SeedRead(ConnectionTable, NullScopePartition);
        var builds = 0;

        async Task<IGenericResult<string>> Build()
        {
            builds++;
            _ = await Read(gateway, ConnectionTable);
            return GenericResult<string>.Success("empty");
        }

        var first = await gateway.Derive<string>("store", Build, static _ => false, TestContext.Current.CancellationToken);
        _ = await gateway.Derive<string>("store", Build, static _ => false, TestContext.Current.CancellationToken);

        first.Value.ShouldBe("empty");
        builds.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailedBuildIsReturnedAndNotKept()
    {
        var gateway = GatewayFor();
        SeedRead(ConnectionTable, NullScopePartition);
        var builds = 0;

        async Task<IGenericResult<string>> Build()
        {
            builds++;
            _ = await Read(gateway, ConnectionTable);
            return GenericResult<string>.Failure(new GenericMessage("the store could not be built"));
        }

        var first = await gateway.Derive<string>("store", Build, static _ => true, TestContext.Current.CancellationToken);
        _ = await gateway.Derive<string>("store", Build, static _ => true, TestContext.Current.CancellationToken);

        first.IsSuccess.ShouldBeFalse();
        builds.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AGatewayWithTheCacheOffBuildsEveryTime()
    {
        var gateway = GatewayFor(cacheEnabled: false);
        var builds = 0;

        Task<IGenericResult<string>> Build()
        {
            builds++;
            return Task.FromResult<IGenericResult<string>>(GenericResult<string>.Success("store"));
        }

        _ = await gateway.Derive<string>("store", Build, static _ => true, TestContext.Current.CancellationToken);
        _ = await gateway.Derive<string>("store", Build, static _ => true, TestContext.Current.CancellationToken);

        builds.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CallersInDifferentPartitionsDoNotShareADerivedValue()
    {
        var first = AccessorFor(Guid.NewGuid(), Guid.NewGuid());
        var second = AccessorFor(Guid.NewGuid(), Guid.NewGuid());
        var firstPartition = ConnectionTypes.ByName("MsSql").CachePartition(first.Current);
        var secondPartition = ConnectionTypes.ByName("MsSql").CachePartition(second.Current);

        // Without this the test would pass for the wrong reason: two callers that share a partition share a value.
        firstPartition.ShouldNotBe(secondPartition);

        SeedRead(ConnectionTable, firstPartition);
        SeedRead(ConnectionTable, secondPartition);
        var firstGateway = GatewayFor(first);
        var secondGateway = GatewayFor(second);
        var builds = 0;

        async Task<IGenericResult<string>> BuildWith(ConfigurationGateway gateway)
        {
            builds++;
            _ = await Read(gateway, ConnectionTable);
            return GenericResult<string>.Success("store");
        }

        _ = await firstGateway.Derive<string>("store", () => BuildWith(firstGateway), static _ => true, TestContext.Current.CancellationToken);
        _ = await secondGateway.Derive<string>("store", () => BuildWith(secondGateway), static _ => true, TestContext.Current.CancellationToken);
        builds.ShouldBe(2, "each caller's partition builds the value it is allowed to see");

        _ = await firstGateway.Derive<string>("store", () => BuildWith(firstGateway), static _ => true, TestContext.Current.CancellationToken);
        _ = await secondGateway.Derive<string>("store", () => BuildWith(secondGateway), static _ => true, TestContext.Current.CancellationToken);
        builds.ShouldBe(2, "and each partition keeps its own");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ConcurrentCallersForOneNameShareOneBuild()
    {
        var gateway = GatewayFor();
        SeedRead(ConnectionTable, NullScopePartition);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var builds = 0;

        async Task<IGenericResult<string>> Build()
        {
            builds++;
            _ = await Read(gateway, ConnectionTable);
            await release.Task;
            return GenericResult<string>.Success("store");
        }

        var first = gateway.Derive<string>("store", Build, static _ => true, TestContext.Current.CancellationToken);
        var second = gateway.Derive<string>("store", Build, static _ => true, TestContext.Current.CancellationToken);
        release.SetResult();

        (await first).Value.ShouldBe("store");
        (await second).Value.ShouldBe("store");
        builds.ShouldBe(1);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private ConfigurationGateway GatewayFor(IAuthenticationContextAccessor? accessor = null, bool cacheEnabled = true)
        => new(
            "PlatformConfiguration",
            _connections.Object,
            _schema,
            NullLogger<ConfigurationGateway>.Instance,
            _cache,
            new MainDataGatewayConfiguration { EnableCache = cacheEnabled },
            accessor);

    private Task<IGenericResult<IEnumerable<string>>> Read(ConfigurationGateway gateway, DataStoreTarget target)
        => gateway.Execute<IEnumerable<string>>(_read.Object, target, TestContext.Current.CancellationToken);

    // The entry the gateway will look for when the build reads this container as this caller.
    private void SeedRead(DataStoreTarget target, string partition)
        => _cache.Set(
            string.Concat(partition, "|_cfg|", CacheKeyBuilder.ComputeCacheKey(_read.Object, target), ":", typeof(IEnumerable<string>).FullName),
            GenericResult<IEnumerable<string>>.Success(["row"]),
            [CacheKeyBuilder.TagFor(target)],
            TimeSpan.FromMinutes(5));

    // The user id is a Guid because the MsSql scheme routes a caller to the per-user session context only
    // when it parses as one; anything else shares a constant partition and two callers would look alike.
    private static IAuthenticationContextAccessor AccessorFor(Guid tenantId, Guid userId)
    {
        var context = new Mock<IAuthenticationContext>();
        context.SetupGet(c => c.IsAuthenticated).Returns(true);
        context.SetupGet(c => c.UserId).Returns(userId.ToString());
        context.SetupGet(c => c.ActiveTenantId).Returns(tenantId);
        context.SetupGet(c => c.ActiveOrgId).Returns((Guid?)null);
        context.SetupGet(c => c.IsSystemContext).Returns(false);
        context.SetupGet(c => c.Roles).Returns([]);
        context.SetupGet(c => c.Permissions).Returns([]);
        context.SetupGet(c => c.Claims).Returns(new Dictionary<string, object>());

        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(context.Object);
        return accessor.Object;
    }
}
