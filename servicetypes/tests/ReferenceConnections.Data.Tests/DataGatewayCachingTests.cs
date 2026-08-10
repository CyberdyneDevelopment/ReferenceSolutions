using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Abstractions.Caching;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Caching;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Tests for the built-in caching behaviour in <see cref="DataGatewayService"/>.
/// </summary>
/// <remarks>
/// Covers:
/// <list type="bullet">
///   <item>Cache hit short-circuits before reaching ExecuteCore (connection not called).</item>
///   <item><c>useCache:false</c> skips cache read but writes on success; next default call hits cache.</item>
///   <item><c>EnableCache:false</c> produces a fully cacheless path — every call reaches the connection.</item>
///   <item>Cross-tenant isolation: entries keyed for tenant A are invisible to tenant B.</item>
///   <item>RLS compliance: the fresh path (connection.Execute) runs on every cache miss and bypass, not on hits.</item>
/// </list>
/// </remarks>
[Collection(nameof(DataServiceTestCollection))]
public sealed class DataGatewayCachingTests
{
    private readonly Mock<IDataConnectionProvider> _connectionProviderMock;
    private readonly Mock<IDataSetConfigurationProvider> _dataSetProviderMock;
    private readonly Mock<DataStoreConfigurationProvider> _dataStoreConfigProviderMock;
    private readonly Mock<IDataStoreProvider> _dataStoreProviderMock;
    private readonly Mock<ConnectionConfigurationProvider> _connectionConfigProviderMock;
    // Why: Real DataGatewayResultCache + real IMemoryCache so cache mechanics (tag tracking,
    // eviction, TryGet/Set) work correctly. Mocking the cache would hide implementation bugs.
    private readonly DataGatewayResultCache _cache;

    public DataGatewayCachingTests()
    {
        _connectionProviderMock = new Mock<IDataConnectionProvider>();

        _dataSetProviderMock = new Mock<IDataSetConfigurationProvider>();
        // Why: DataGatewayService probes the DataSet provider on DataSetTarget execute calls.
        // Without a default setup Moq returns null Task causing NRE at the await site.
        _dataSetProviderMock
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataSetConfiguration>.Failure());

        // Why: DataStoreConfigurationProvider requires four explicit constructor args.
        // Castle DynamicProxy does not infer optional params.
        _dataStoreConfigProviderMock = new Mock<DataStoreConfigurationProvider>(
            NullLogger<DataStoreConfigurationProvider>.Instance,
            new Lazy<Fdw.Services.Data.Abstractions.IConfigurationGateway>(() => null!),
            new Lazy<Fdw.Services.Configuration.DefaultConfigurationProvider<Fdw.Services.Connections.DataContainerConfiguration, Fdw.Services.Connections.Commands.DataContainerConfigurationCommand>>(() => null!),
            "ConfigurationDb",
            "data",
            null!) { CallBase = false };
        _dataStoreConfigProviderMock
            .Setup(m => m.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataStoreConfiguration>>.Success(new List<DataStoreConfiguration>()));
#pragma warning disable CS8620 // Why: Nullable wrapping is intentional — "not found" default setup
        _dataStoreConfigProviderMock
            .Setup(m => m.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration?>.Success(null));
#pragma warning restore CS8620

        // Why the header and not a typed body: the gateway reads the connection HEADER and dispatches
        // on its ServiceOptionType. A typed body (MsSqlConnectionConfiguration) implements the marker
        // interface rather than deriving from the header, so it is not what this provider returns.
        // "MsSql" is what makes the lookup land on a kind whose session contexts compose a per-caller
        // partition — the behaviour these tests are about.
        _connectionConfigProviderMock = new Mock<ConnectionConfigurationProvider>(
            NullLogger<ConnectionConfigurationProvider>.Instance,
            new Lazy<Fdw.Services.Data.Abstractions.IConfigurationGateway>(() => null!),
            "ConfigurationDb",
            "conn",
            null!) { CallBase = false };
        _connectionConfigProviderMock
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConnectionConfiguration>.Success(
                new ConnectionConfiguration { ServiceOptionType = "MsSql" }));

        _dataStoreProviderMock = new Mock<IDataStoreProvider>();
        // Why: default to "not found" so unconfigured container routing fails loud instead of NRE.
        _dataStoreProviderMock
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Failure(new GenericMessage("container not found")));

        _cache = new DataGatewayResultCache(
            new MemoryCache(new MemoryCacheOptions()),
            NullLoggerFactory.Instance);
    }

    // =========================================================================
    // Test 1: Cache hit — connection.Execute is never called
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CacheHit_ReturnsWithoutCallingConnectionExecute()
    {
        // Arrange
        var target = new DataStoreTarget("test-store", null, "TestContainer");
        var commandMock = BuildCachingCommand();

        // Why the entry is written through the service rather than placed at a computed key: the key
        // carries a partition the connection kind composes from the calling scope, so a test that
        // spells the key itself is asserting today's partition format, not the caching behaviour.
        // Executing once populates it at whatever key the gateway actually uses.
        var connectionId = BuildDataStoreTree("test-store", "TestContainer");
        BuildSuccessConnection("cached-value", connectionId);

        var service = BuildCacheEnabledService();
        var seeded = await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);
        seeded.IsSuccess.ShouldBeTrue();
        _connectionProviderMock.Invocations.Clear();

        // Act — the same question again, which must be served from cache
        var result = await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("cached-value");
        // Why: the cache hit happens before container or connection resolution;
        // _connectionProviderMock.Get() is the earliest point in ExecuteCore that could be called.
        _connectionProviderMock.Verify(
            p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "connection provider must not be consulted on a cache hit");
    }

    // =========================================================================
    // Test 2: useCache:false — skips cache read, writes result, next default call hits cache
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task UseCacheFalse_SkipsCacheRead_WritesToCache_NextDefaultReadHitsCache()
    {
        // Arrange
        var connectionId = BuildDataStoreTree("test-store", "TestContainer");
        var connectionMock = BuildSuccessConnection("fresh-value", connectionId);
        var target = new DataStoreTarget("test-store", null, "TestContainer");
        var commandMock = BuildCachingCommand();

        var service = BuildCacheEnabledService();

        // Why the stale entry is written through the service: the cache key carries a partition the
        // connection kind composes, so spelling the key here would test the partition format instead
        // of the force-refresh behaviour. One execution against a connection returning the stale value
        // puts it at the key the gateway itself uses; the connection then starts returning the fresh
        // one, so a bypassed read and a served read are distinguishable by value alone.
        connectionMock
            .Setup(c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("stale-value"));
        var seeded = await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);
        seeded.IsSuccess.ShouldBeTrue();
        seeded.Value.ShouldBe("stale-value");

        connectionMock
            .Setup(c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("fresh-value"));
        connectionMock.Invocations.Clear();

        // Act 1 — force-refresh: useCache:false bypasses the stale cache read and writes the fresh result
        var forceRefreshResult = await service.Execute<string>(commandMock.Object, target, useCache: false, TestContext.Current.CancellationToken);

        // Act 2 — default Execute (useCache:true) reads from the cache written by Act 1
        var defaultResult = await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        forceRefreshResult.IsSuccess.ShouldBeTrue();
        // Why: useCache:false bypasses the stale pre-seeded value; connection returns "fresh-value".
        forceRefreshResult.Value.ShouldBe("fresh-value", "useCache:false must bypass stale cache and return fresh connection result");

        defaultResult.IsSuccess.ShouldBeTrue();
        // Why: the default read after the force-refresh must hit the cache written by useCache:false,
        // proving that useCache:false still writes on success (it only skips the READ).
        defaultResult.Value.ShouldBe("fresh-value", "default read after useCache:false must return the refreshed cache entry, not the original stale value");

        // Why: connection.Execute is called exactly once — on the useCache:false force-refresh.
        // The default read (Act 2) was a cache hit; the connection was not consulted again.
        connectionMock.Verify(
            c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "connection.Execute must be called exactly once: on the useCache:false force-refresh, not on the subsequent cache hit");
    }

    // =========================================================================
    // Test 3: EnableCache:false — every Execute reaches the connection (cacheless)
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task EnableCacheFalse_CallsConnectionOnEveryExecute()
    {
        // Arrange
        var connectionId = BuildDataStoreTree("test-store", "TestContainer");
        var connectionMock = BuildSuccessConnection("result", connectionId);
        var target = new DataStoreTarget("test-store", null, "TestContainer");
        var commandMock = BuildCachingCommand();

        // Why: EnableCache=false means the gateway neither reads from nor writes to the cache.
        // Every Execute goes to ExecuteCore regardless of what the cache contains.
        var service = BuildCacheEnabledService(enableCache: false);

        // Act — call Execute twice
        var result1 = await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);
        var result2 = await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result1.IsSuccess.ShouldBeTrue();
        result2.IsSuccess.ShouldBeTrue();
        // Why: EnableCache=false bypasses both cache read and write on every call.
        // connection.Execute must run twice — there is no cache to short-circuit either call.
        connectionMock.Verify(
            c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2),
            "EnableCache:false must call connection.Execute on every Execute call — no caching of any kind");
    }

    // =========================================================================
    // Test 4: Cross-tenant isolation — entries for tenant A are invisible to tenant B
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CrossTenantIsolation_TenantAEntryNotVisibleToTenantB()
    {
        // Arrange — one query, two callers whose only difference is the tenant they are active in.
        var target = new DataStoreTarget("test-store", null, "TestContainer");
        var commandMock = BuildCachingCommand();
        var connectionId = BuildDataStoreTree("test-store", "TestContainer");
        var connectionMock = BuildSuccessConnection("fresh-per-tenant", connectionId);

        var tenantA = AccessorFor(Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"));
        var tenantB = AccessorFor(Guid.Parse("22222222-2222-2222-2222-222222222222"), Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"));

        var serviceA = BuildCacheEnabledService(authenticationContextAccessor: tenantA);
        var serviceB = BuildCacheEnabledService(authenticationContextAccessor: tenantB);

        // Act — A populates the cache, then B asks the same question.
        var firstA = await serviceA.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);
        var secondA = await serviceA.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);
        var firstB = await serviceB.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert — every read succeeds and returns the same value; the point is where it came from.
        firstA.IsSuccess.ShouldBeTrue();
        secondA.IsSuccess.ShouldBeTrue();
        firstB.IsSuccess.ShouldBeTrue();

        // Why the count is the assertion: A's second read is a hit, so it does not reach the
        // connection. B's first read is a miss despite asking the identical question, because the
        // entry A wrote is keyed to A's scope. Two executions, not one and not three: one for A's
        // miss, one for B's, none for A's hit.
        connectionMock.Verify(
            c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2),
            "tenant B must not read the entry tenant A wrote — the cache key carries the caller's scope, "
            + "and A's own repeat must still hit, or the test proves nothing about isolation");
    }

    // =========================================================================
    // Test 5: RLS compliance — fresh path (connection.Execute) runs on every miss and
    // useCache:false bypass, but NOT on cache hits
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RLS_FreshPathRunsOnEveryMissAndBypass_NotOnCacheHit()
    {
        // Arrange
        var connectionId = BuildDataStoreTree("test-store", "TestContainer");
        var connectionMock = BuildSuccessConnection("result", connectionId);
        var target = new DataStoreTarget("test-store", null, "TestContainer");
        var commandMock = BuildCachingCommand();
        var service = BuildCacheEnabledService();

        // Act 1 — first Execute is a cache miss → fresh path → connection.Execute called (1)
        await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Act 2 — useCache:false bypass → fresh path → connection.Execute called (2)
        // Why: after Act 1, the cache has an entry. useCache:false skips the READ but
        // still runs ExecuteCore (and writes the fresh result back — "always write on enable").
        await service.Execute<string>(commandMock.Object, target, useCache: false, TestContext.Current.CancellationToken);

        // Act 3 — default Execute after the force-refresh → cache hit → fresh path NOT taken
        await service.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        // Why: The RLS SET SESSION_CONTEXT call lives inside connection.Execute (the connection
        // implementation). The gateway guarantees RLS runs on every fresh path (miss or bypass).
        // It must NOT run on a cache hit (Act 3) — total = exactly 2 times.
        connectionMock.Verify(
            c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2),
            "RLS (connection.Execute) must run on cache miss (Act 1) and useCache:false bypass (Act 2), but NOT on cache hit (Act 3)");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    // Why the connection config provider is always supplied: the gateway resolves the store's
    // connection kind to get a cache partition, and fails the read outright when it cannot — without
    // a partition it cannot tell which callers may share a result. A test that omits it exercises
    // that failure path, not caching.
    private DataGatewayService BuildCacheEnabledService(
        bool enableCache = true,
        IAuthenticationContextAccessor? authenticationContextAccessor = null)
        => new DataGatewayService(
            NullLoggerFactory.Instance,
            _connectionProviderMock.Object,
            new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object),
            _dataStoreConfigProviderMock.Object,
            dataStoreProvider: _dataStoreProviderMock.Object,
            cache: _cache,
            options: Options.Create(new DataGatewayOptions { EnableCache = enableCache }),
            authenticationContextAccessor: authenticationContextAccessor,
            connectionConfigProvider: _connectionConfigProviderMock.Object);

    /// <summary>
    /// An accessor whose Current is a context active in the given tenant. The partition the MsSql
    /// scheme composes includes the tenant, so two of these with different ids are two scopes.
    /// </summary>
    // Why the user id is a Guid: the MsSql scheme routes a caller to ForUserSessionContext only when
    // IsResolvedUser holds, and that requires UserId to parse as one. A non-Guid falls to
    // DenySessionContext, whose partition is constant — so two "different" tenants would share a cache
    // entry and this test would pass for the wrong reason.
    private static IAuthenticationContextAccessor AccessorFor(Guid tenantId, Guid userId)
    {
        var context = new Mock<IAuthenticationContext>();
        context.SetupGet(c => c.IsAuthenticated).Returns(true);
        context.SetupGet(c => c.UserId).Returns(userId.ToString());
        context.SetupGet(c => c.ActiveTenantId).Returns(tenantId);
        context.SetupGet(c => c.ActiveOrgId).Returns((Guid?)null);
        context.SetupGet(c => c.IsCrossTenant).Returns(false);
        context.SetupGet(c => c.IsSystemContext).Returns(false);
        context.SetupGet(c => c.Roles).Returns([]);
        context.SetupGet(c => c.Permissions).Returns([]);
        context.SetupGet(c => c.Claims).Returns(new Dictionary<string, object>());

        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(context.Object);
        return accessor.Object;
    }

    // Why: Sets up _dataStoreProviderMock and _dataStoreConfigProviderMock so that
    // ExecuteCore can resolve the container and the DataStore's ConnectionId.
    private Guid BuildDataStoreTree(string storeName, string containerName)
    {
        var mockContainer = new Mock<IDataContainer>();
        mockContainer.Setup(c => c.Name).Returns(containerName);
        mockContainer.Setup(c => c.Keys).Returns((IReadOnlyList<IContainerKey>)Array.Empty<IContainerKey>());
        mockContainer.Setup(c => c.Nodes).Returns((IReadOnlyList<IDataNode>)Array.Empty<IDataNode>());
        mockContainer.Setup(c => c.Description).Returns((string?)null);

        var connectionId = Guid.NewGuid();

        _dataStoreProviderMock
            .Setup(p => p.Get(
                It.Is<string>(n => string.Equals(n, storeName, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<string>(),
                It.Is<string>(n => string.Equals(n, containerName, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Success(mockContainer.Object));

        var storeConfig = new DataStoreConfiguration { Name = storeName, ConnectionId = connectionId };
        _dataStoreConfigProviderMock
            .Setup(m => m.Get(storeName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Success(storeConfig));

        return connectionId;
    }

    // Why: Returns a Mock<IDataConnection> pre-registered with the connection provider.
    // When connectionId is provided, the provider is set up to return this mock only for that ID.
    private Mock<IDataConnection> BuildSuccessConnection(string returnValue, Guid connectionId)
    {
        var connectionMock = new Mock<IDataConnection>();
        connectionMock
            .Setup(c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success(returnValue));

        _connectionProviderMock
            .Setup(p => p.Get(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(connectionMock.Object));

        return connectionMock;
    }

    private static Mock<IDataCommand> BuildCachingCommand()
    {
        var mock = new Mock<IDataCommand>();
        mock.Setup(c => c.CommandType).Returns("Query");
        // Why: CachePolicy.IsEnabled checks Metadata[CacheEnabledKey] == true.
        // DataGatewayService.Execute only activates caching when this is true AND EnableCache=true.
        mock.Setup(c => c.Metadata).Returns(new Dictionary<string, object>
        {
            [CachePolicy.CacheEnabledKey] = (object)true
        });
        return mock;
    }
}
