using System;
using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Caching;
using Fdw.Services.Data.Configuration;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Verifies that ConfigurationGateway.Execute&lt;T&gt; caches reads in the shared
/// DataGatewayResultCache and that tag-based invalidation (the path taken by
/// provider.Save/Delete) evicts the matching entries.
/// </summary>
[Collection(nameof(DataServiceTestCollection))]
public sealed class ConfigurationGatewayCachingTests
{
    // Why: Real DataGatewayResultCache + IMemoryCache so we exercise actual tag-tracking and
    // eviction mechanics without mocking internal implementation details.
    private readonly DataGatewayResultCache _cache;
    private readonly Mock<IConnectionFactory> _factoryMock;
    private readonly ConfigurationSchema _emptySchema;

    // Why the partition is asked of the connection kind rather than written out: it is the kind that
    // composes it, from whatever the calling scope turns out to be. Spelling it here would pin the
    // test to today's format and start failing the next time the scheme changes, which is not what
    // these tests are about. No accessor is supplied to the gateway, so the scope is the null one.
    private static readonly string _partition =
        ConnectionTypes.ByName("MsSql").CachePartition(null);

    public ConfigurationGatewayCachingTests()
    {
        _cache = new DataGatewayResultCache(
            new MemoryCache(new MemoryCacheOptions()),
            NullLoggerFactory.Instance);

        _factoryMock = new Mock<IConnectionFactory>();
        // Why Create is configured rather than left bare: the schema below declares a connection, so
        // BuildConnection now reaches the factory instead of stopping at "no ConfigurationDb entry". A
        // bare Mock returns a null Task from Create, and awaiting that throws NullReferenceException
        // inside the gateway — an exception from the harness, not a result from the code under test.
        // Returning a failure gives BuildConnection the clean miss these tests are written against.
        _factoryMock
            .Setup(f => f.Create(It.IsAny<IGenericConfiguration>(), It.IsAny<ISecretManager?>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(GenericResult<IGenericConnection>.Failure(new GenericMessage("no connection in this test")));

        // Why the schema declares a connection rather than being empty: the gateway resolves the
        // ConfigurationDb connection's kind before it touches the cache, because the kind is what
        // computes the partition the key is built from. Without one it fails the read outright — by
        // design, since it cannot otherwise tell which callers may share a result. An empty schema
        // therefore never reaches the cache at all, and a caching test against it proves nothing.
        //
        // Nothing beyond the kind is needed: BuildConnection still fails before IConnectionFactory
        // .Create, so cache-hit tests short-circuit and cache-miss tests get a clean failure.
        _emptySchema = new ConfigurationSchema
        {
            Connections = { new ConnectionConfiguration { Name = "ConfigurationDb", ServiceOptionType = "MsSql" } },
        };
    }

    // =========================================================================
    // Cache hit — pre-seeded entry returned without reaching ExecuteCore
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteWithCacheEnabled_ReturnsCachedResult_WhenKeyIsPresent()
    {
        // Arrange
        var options = Options.Create(new DataGatewayOptions { EnableCache = true });
        var gateway = new ConfigurationGateway(
            _factoryMock.Object,
            _emptySchema,
            NullLogger<ConfigurationGateway>.Instance,
            _cache,
            options);

        var commandMock = BuildQueryCommand();
        var target = new DataStoreTarget("ConfigurationDb", "conn", "Connection");

        // Pre-seed the cache at the exact key ConfigurationGateway will compute for Execute<IEnumerable<string>>.
        var expectedKey = string.Concat(
            _partition, "|_cfg|",
            CacheKeyBuilder.ComputeCacheKey(commandMock.Object, target),
            ":",
            typeof(IEnumerable<string>).FullName);
        var cachedResult = GenericResult<IEnumerable<string>>.Success(["cached-value"]);
        _cache.Set(expectedKey, cachedResult, ["conn.Connection"], TimeSpan.FromMinutes(5));

        // Act — cache hit; ExecuteCore must NOT be reached
        var result = await gateway.Execute<IEnumerable<string>>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert — result is exactly the pre-seeded value, proving the short-circuit
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Single().ShouldBe("cached-value");
        // Why: IConnectionFactory.Create is never called when the cache returns a hit —
        // the factory is only used by BuildConnection inside ExecuteCore, which is bypassed.
        _factoryMock.Verify(
            f => f.Create(It.IsAny<IGenericConfiguration>(), It.IsAny<ISecretManager?>(), It.IsAny<System.Threading.CancellationToken>()),
            Times.Never);
    }

    // =========================================================================
    // Tag invalidation — simulates what a provider.Save() does
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ConfigWriteInvalidatesCachedConfigRead()
    {
        // Arrange — gateway with caching enabled
        var options = Options.Create(new DataGatewayOptions { EnableCache = true });
        var gateway = new ConfigurationGateway(
            _factoryMock.Object,
            _emptySchema,
            NullLogger<ConfigurationGateway>.Instance,
            _cache,
            options);

        var commandMock = BuildQueryCommand();
        var target = new DataStoreTarget("ConfigurationDb", "conn", "Connection");

        // Pre-seed the cache — represents a previously cached config read.
        var cacheKey = string.Concat(
            _partition, "|_cfg|",
            CacheKeyBuilder.ComputeCacheKey(commandMock.Object, target),
            ":",
            typeof(string).FullName);
        _cache.Set(cacheKey, GenericResult<string>.Success("pre-write-value"), ["conn.Connection"], TimeSpan.FromMinutes(5));

        // Baseline — verify the pre-seeded value is returned (cache hit)
        var beforeInvalidation = await gateway.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);
        beforeInvalidation.IsSuccess.ShouldBeTrue("cached value should be returned before invalidation");
        beforeInvalidation.Value.ShouldBe("pre-write-value");

        // Act — provider.Save calls ICacheInvalidator.InvalidateByTag(Commands().CacheTag("conn"))
        // ConfigurationCommandBase.CacheTag("conn") returns "conn.Connection" for the Connection table.
        _cache.InvalidateByTag("conn.Connection");

        // After invalidation: cache miss → ExecuteCore → BuildConnection fails (no connections in schema)
        var afterInvalidation = await gateway.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert — result is a fresh failure, not the pre-seeded stale value
        // Why: do NOT access .Value on a failed result — GenericResult throws InvalidOperationException.
        afterInvalidation.IsSuccess.ShouldBeFalse(
            "cache was evicted by InvalidateByTag; ExecuteCore returns failure when schema has no ConfigurationDb connection");
    }

    // =========================================================================
    // EnableCache=false — cache reads and writes are skipped entirely
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteWithCacheDisabled_DoesNotReadFromCache_EvenWhenKeyIsPresent()
    {
        // Arrange — gateway with caching DISABLED
        var options = Options.Create(new DataGatewayOptions { EnableCache = false });
        var gateway = new ConfigurationGateway(
            _factoryMock.Object,
            _emptySchema,
            NullLogger<ConfigurationGateway>.Instance,
            _cache,
            options);

        var commandMock = BuildQueryCommand();
        var target = new DataStoreTarget("ConfigurationDb", "conn", "Connection");

        // Pre-seed the cache — should be ignored when EnableCache=false.
        var cacheKey = string.Concat(
            _partition, "|_cfg|",
            CacheKeyBuilder.ComputeCacheKey(commandMock.Object, target),
            ":",
            typeof(string).FullName);
        _cache.Set(cacheKey, GenericResult<string>.Success("should-not-be-returned"), ["conn.Connection"], TimeSpan.FromMinutes(5));

        // Act
        var result = await gateway.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert — ExecuteCore was reached (cache read bypassed); BuildConnection fails (schema empty)
        result.IsSuccess.ShouldBeFalse(
            "EnableCache=false bypasses cache; ExecuteCore returns failure when schema has no ConfigurationDb connection");
    }

    // =========================================================================
    // useCache=false — skips cache read but writes on success; fresh path taken
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteWithUseCacheFalse_SkipsCacheRead_GoesToFreshPath()
    {
        // Arrange — caching enabled, but call overrides to skip cache read
        var options = Options.Create(new DataGatewayOptions { EnableCache = true });
        var gateway = new ConfigurationGateway(
            _factoryMock.Object,
            _emptySchema,
            NullLogger<ConfigurationGateway>.Instance,
            _cache,
            options);

        var commandMock = BuildQueryCommand();
        var target = new DataStoreTarget("ConfigurationDb", "conn", "Connection");

        // Pre-seed the cache — should be SKIPPED when useCache=false.
        var cacheKey = string.Concat(
            _partition, "|_cfg|",
            CacheKeyBuilder.ComputeCacheKey(commandMock.Object, target),
            ":",
            typeof(string).FullName);
        _cache.Set(cacheKey, GenericResult<string>.Success("stale-cached-value"), ["conn.Connection"], TimeSpan.FromMinutes(5));

        // Act — force-refresh: cache read is bypassed, fresh ExecuteCore is attempted
        var result = await gateway.Execute<string>(commandMock.Object, target, useCache: false, TestContext.Current.CancellationToken);

        // Assert — stale cached value NOT returned; ExecuteCore fails because schema is empty
        // Why: do NOT access .Value on a failed result — GenericResult throws InvalidOperationException.
        result.IsSuccess.ShouldBeFalse(
            "useCache=false bypasses cache read; ExecuteCore returns failure when schema has no ConfigurationDb connection");
    }

    // =========================================================================
    // Cache tag format matches provider invalidation convention
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CacheTagFormat_MatchesProviderInvalidationConvention()
    {
        // Arrange — command + target for conn.Connection (standard config path)
        var commandMock = BuildQueryCommand();
        var target = new DataStoreTarget("ConfigurationDb", "conn", "Connection");

        // Act — tags ConfigurationGateway stores when caching this read
        var tags = CacheKeyBuilder.GetInvalidationTags(commandMock.Object, target);

        // Assert — tag must match ConfigurationCommandBase.CacheTag("conn") for the Connection table,
        // which returns string.Concat("conn", ".", "Connection") = "conn.Connection".
        // Provider.Save calls: ICacheInvalidator.InvalidateByTag(Commands().CacheTag("conn"))
        // CacheKeyBuilder default is "{path}.{container}" = "{target.Path}.{target.Container}".
        tags.ShouldNotBeEmpty();
        tags.ShouldContain("conn.Connection",
            "CacheKeyBuilder default tag '{path}.{container}' must match ConfigurationCommandBase.CacheTag(pathName) format so provider writes evict the cached reads");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static Mock<IDataCommand> BuildQueryCommand()
    {
        var mock = new Mock<IDataCommand>();
        mock.Setup(c => c.CommandType).Returns("Query");
        // Why: empty Metadata so CacheKeyBuilder falls back to target-derived key prefix and tag.
        mock.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        return mock;
    }
}
