using System;
using System.Collections.Generic;
using System.Security.Claims;
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
using Microsoft.AspNetCore.Http;
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

        // Pre-seed the cache at the exact key the gateway computes.
        // TenantDiscriminator() = "_" when no IHttpContextAccessor is provided.
        var cacheKey = string.Concat(
            "_|",
            CacheKeyBuilder.ComputeCacheKey(commandMock.Object, target),
            ":", typeof(string).FullName);
        _cache.Set(cacheKey, GenericResult<string>.Success("cached-value"), ["test-store.TestContainer"], TimeSpan.FromMinutes(5));

        var service = BuildCacheEnabledService();

        // Act — cache hit should be returned without reaching ExecuteCore
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

        // Pre-seed the cache with a stale value — useCache:false must bypass this read.
        var cacheKey = string.Concat(
            "_|",
            CacheKeyBuilder.ComputeCacheKey(commandMock.Object, target),
            ":", typeof(string).FullName);
        _cache.Set(cacheKey, GenericResult<string>.Success("stale-value"), ["test-store.TestContainer"], TimeSpan.FromMinutes(5));

        var service = BuildCacheEnabledService();

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
        // Arrange — pre-seed cache for the "no-context" partition (discriminator key = "_")
        var target = new DataStoreTarget("test-store", null, "TestContainer");
        var commandMock = BuildCachingCommand();

        var noContextCacheKey = string.Concat(
            "_|",
            CacheKeyBuilder.ComputeCacheKey(commandMock.Object, target),
            ":", typeof(string).FullName);
        _cache.Set(noContextCacheKey, GenericResult<string>.Success("no-context-value"), ["test-store.TestContainer"], TimeSpan.FromMinutes(5));

        // Set up the data store tree so tenant B's cache miss can complete a fresh execution.
        var connectionId = BuildDataStoreTree("test-store", "TestContainer");
        var connectionMock = BuildSuccessConnection("tenant-b-fresh", connectionId);

        // Why: mock an HTTP context carrying tenant_id="tenant-b", org_id="org-1" so
        // TenantDiscriminator() returns "tenant-b/org-1" (different from the no-context "_").
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("tenant_id", "tenant-b"),
            new Claim("org_id", "org-1")
        ]));
        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(c => c.User).Returns(claimsPrincipal);
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(a => a.HttpContext).Returns(httpContextMock.Object);

        // Tenant B service: discriminator "tenant-b/org-1"
        var tenantBService = BuildCacheEnabledService(httpContextAccessor: accessorMock.Object);
        // No-context service: discriminator "_" — matches the pre-seeded key
        var noContextService = BuildCacheEnabledService();

        // Act 1 — tenant B should NOT get the no-context cache entry (different tenant key → cache miss)
        var tenantBResult = await tenantBService.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Act 2 — no-context service SHOULD hit the pre-seeded entry (same "_" key)
        var noContextResult = await noContextService.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert — tenant B missed the cache and got a fresh connection result
        tenantBResult.IsSuccess.ShouldBeTrue();
        tenantBResult.Value.ShouldBe("tenant-b-fresh",
            "tenant B must not receive the entry keyed for tenant _ — the cache is tenant-discriminated");

        // Why: connection.Execute was called exactly once, for the tenant-B cache miss.
        // Act 2 (no-context) was a cache hit, so the connection was not called again.
        connectionMock.Verify(
            c => c.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "connection.Execute must be called once for the tenant-B miss; no-context call hits the pre-seeded cache entry");

        // Assert — no-context service got the pre-seeded value (same tenant key "_")
        noContextResult.IsSuccess.ShouldBeTrue();
        noContextResult.Value.ShouldBe("no-context-value",
            "no-context service must hit the pre-seeded cache entry keyed for tenant _");
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

    private DataGatewayService BuildCacheEnabledService(
        bool enableCache = true,
        IHttpContextAccessor? httpContextAccessor = null)
        => new DataGatewayService(
            NullLoggerFactory.Instance,
            _connectionProviderMock.Object,
            new Lazy<IDataSetConfigurationProvider>(() => _dataSetProviderMock.Object),
            _dataStoreConfigProviderMock.Object,
            httpContextAccessor: httpContextAccessor,
            dataStoreProvider: _dataStoreProviderMock.Object,
            cache: _cache,
            options: Options.Create(new DataGatewayOptions { EnableCache = enableCache }));

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
