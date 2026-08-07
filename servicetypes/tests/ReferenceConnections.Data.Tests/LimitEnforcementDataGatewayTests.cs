using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Limits;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Tests for <see cref="LimitEnforcementDataGateway"/> — the per-connection rate/concurrency/
/// result-size/daily-budget/timeout enforcement decorator wrapped around an inner <see cref="IDataGateway"/>.
/// </summary>
/// <remarks>
/// Covers:
/// <list type="bullet">
///   <item>Pre-cancellation short-circuit.</item>
///   <item>Resolver-failure and empty-limits passthrough to the inner gateway.</item>
///   <item>Rate-limit, concurrency-limit, max-result-size, and daily-budget trips (each returns a
///   structured failure, never throws, and never reaches the inner gateway).</item>
///   <item>Query timeout — the inner cancellation is translated into a structured failure.</item>
///   <item>Successful execution increments the daily counters exactly when a daily-budget limit is configured.</item>
///   <item>DataSetTarget, BeginTransaction, and OpenRecordSource bypass limit enforcement entirely.</item>
/// </list>
/// </remarks>
[Collection(nameof(DataServiceTestCollection))]
public sealed class LimitEnforcementDataGatewayTests
{
    private readonly Mock<IDataGateway> _innerMock = new();
    private readonly Mock<IConnectionLimitResolver> _resolverMock = new();
    private readonly ConnectionLimitCounterStore _counters = new();

    // =========================================================================
    // Constructor guards
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new LimitEnforcementDataGateway(null!, _resolverMock.Object, _counters, NullLoggerFactory.Instance));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_NullLimitResolver_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new LimitEnforcementDataGateway(_innerMock.Object, null!, _counters, NullLoggerFactory.Instance));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_NullCounters_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new LimitEnforcementDataGateway(_innerMock.Object, _resolverMock.Object, null!, NullLoggerFactory.Instance));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task Constructor_NullLoggerFactory_FallsBackToNullLoggerFactoryAndOperatesNormally()
    {
        // Arrange — null loggerFactory must fall back to NullLoggerFactory.Instance (the only
        // sanctioned `??` fallback pattern), not throw.
        var target = new DataStoreTarget("conn", null, "Container");
        var commandMock = BuildCommand();
        _resolverMock
            .Setup(r => r.Resolve("conn", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration>()));
        _innerMock
            .Setup(i => i.Execute<string>(commandMock.Object, target, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("ok"));
        var gateway = new LimitEnforcementDataGateway(_innerMock.Object, _resolverMock.Object, _counters, null);

        // Act
        var result = await gateway.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("ok");
    }

    // =========================================================================
    // Pre-cancellation (~:112)
    // =========================================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_CancellationAlreadyRequested_ReturnsOperationCancelledWithoutResolvingOrCallingInner()
    {
        // Arrange
        var gateway = BuildGateway();
        var target = new DataStoreTarget("conn", null, "Container");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await gateway.Execute<string>(BuildCommand().Object, target, cts.Token);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.Count.ShouldBe(1);
        result.Messages[0].Code.ShouldBe("ABSTRACTIONS6-11006");
        _resolverMock.Verify(r => r.Resolve(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _innerMock.Verify(
            i => i.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================================
    // Resolver-fail passthrough (~:121)
    // =========================================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_ResolverFails_PassesThroughDirectlyToInner()
    {
        // Arrange
        var target = new DataStoreTarget("conn", null, "Container");
        var commandMock = BuildCommand();
        _resolverMock
            .Setup(r => r.Resolve("conn", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Failure(new GenericMessage("resolution failed")));
        _innerMock
            .Setup(i => i.Execute<string>(commandMock.Object, target, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("pass-through-value"));
        var gateway = BuildGateway();

        // Act
        var result = await gateway.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("pass-through-value");
        _innerMock.Verify(i => i.Execute<string>(commandMock.Object, target, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // Empty-limits passthrough (~:131)
    // =========================================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_NoLimitsConfigured_PassesThroughDirectlyToInner()
    {
        // Arrange
        var target = new DataStoreTarget("conn", null, "Container");
        var commandMock = BuildCommand();
        _resolverMock
            .Setup(r => r.Resolve("conn", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration>()));
        _innerMock
            .Setup(i => i.Execute<string>(commandMock.Object, target, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("pass-through-value"));
        var gateway = BuildGateway();

        // Act
        var result = await gateway.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("pass-through-value");
        _innerMock.Verify(i => i.Execute<string>(commandMock.Object, target, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // Rate-limit trip (~:225)
    // =========================================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_RateLimitExceeded_SecondCallReturnsRateExceededFailure_InnerCalledOnlyOnce()
    {
        // Arrange — burst of 1 means the first call consumes the only available token; the second
        // call, issued immediately after, finds the bucket empty (refill over microseconds is negligible).
        var connId = Guid.NewGuid();
        var limit = new TestConnectionLimitConfiguration { ConnectionConfigurationId = connId, RateLimit = 1, BurstSize = 1 };
        var target = new DataStoreTarget("conn-rate", null, "Container");
        var commandMock = BuildCommand();
        _resolverMock
            .Setup(r => r.Resolve("conn-rate", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration> { limit }));
        _innerMock
            .Setup(i => i.Execute<string>(commandMock.Object, target, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("ok"));
        var gateway = BuildGateway();

        // Act
        var firstResult = await gateway.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);
        var secondResult = await gateway.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        firstResult.IsSuccess.ShouldBeTrue();
        secondResult.IsSuccess.ShouldBeFalse();
        secondResult.Messages[0].Code.ShouldBe("ABSTRACTIONS6-81000");
        _innerMock.Verify(i => i.Execute<string>(commandMock.Object, target, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // Concurrency-limit trip (~:240)
    // =========================================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_ConcurrencySemaphoreExhausted_ReturnsConcurrencyBlockedFailureWithoutCallingInner()
    {
        // Arrange — CheckConcurrency releases its semaphore immediately after the non-blocking
        // Wait(0) check, so sequential single-threaded calls never naturally collide. Pre-seed the
        // per-connection semaphore (via the internal field — InternalsVisibleTo grants access) fully
        // consumed, to deterministically exercise the trip branch without a timing-dependent race.
        const string connectionName = "conn-concurrency";
        var connId = Guid.NewGuid();
        var limit = new TestConnectionLimitConfiguration { ConnectionConfigurationId = connId, ConcurrencyLimit = 1 };
        _resolverMock
            .Setup(r => r.Resolve(connectionName, It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration> { limit }));
        var gateway = BuildGateway();

        var gatesField = typeof(LimitEnforcementDataGateway)
            .GetField("_concurrencyGates", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var gates = (ConcurrentDictionary<string, SemaphoreSlim>)gatesField.GetValue(gateway)!;
        var exhaustedSemaphore = new SemaphoreSlim(1, 1);
        exhaustedSemaphore.Wait(TestContext.Current.CancellationToken);
        gates[connectionName] = exhaustedSemaphore;

        var target = new DataStoreTarget(connectionName, null, "Container");

        // Act
        var result = await gateway.Execute<string>(BuildCommand().Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("ABSTRACTIONS6-81003");
        _innerMock.Verify(
            i => i.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================================
    // Max-rows on IQueryCommand (~:263)
    // =========================================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_MaxRowsExceededOnQueryCommand_ReturnsMaxResultSizeExceededFailure()
    {
        // Arrange
        var connId = Guid.NewGuid();
        var limit = new TestConnectionLimitConfiguration { ConnectionConfigurationId = connId, MaxRows = 50 };
        var target = new DataStoreTarget("conn-rows", null, "Container");
        var queryCommand = BuildQueryCommandWithPaging(take: 100);
        _resolverMock
            .Setup(r => r.Resolve("conn-rows", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration> { limit }));
        var gateway = BuildGateway();

        // Act
        var result = await gateway.Execute<string>(queryCommand.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("ABSTRACTIONS6-81002");
        _innerMock.Verify(
            i => i.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_MaxRowsWithinLimitOnQueryCommand_PassesThroughToInner()
    {
        // Arrange
        var connId = Guid.NewGuid();
        var limit = new TestConnectionLimitConfiguration { ConnectionConfigurationId = connId, MaxRows = 50 };
        var target = new DataStoreTarget("conn-rows-ok", null, "Container");
        var queryCommand = BuildQueryCommandWithPaging(take: 10);
        _resolverMock
            .Setup(r => r.Resolve("conn-rows-ok", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration> { limit }));
        _innerMock
            .Setup(i => i.Execute<string>(queryCommand.Object, target, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("ok"));
        var gateway = BuildGateway();

        // Act
        var result = await gateway.Execute<string>(queryCommand.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("ok");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_MaxRowsLimitOnNonQueryCommand_PassesThroughToInner()
    {
        // Arrange — limit only applies when the command implements IQueryCommand with Paging set.
        var connId = Guid.NewGuid();
        var limit = new TestConnectionLimitConfiguration { ConnectionConfigurationId = connId, MaxRows = 1 };
        var target = new DataStoreTarget("conn-non-query", null, "Container");
        var commandMock = BuildCommand();
        _resolverMock
            .Setup(r => r.Resolve("conn-non-query", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration> { limit }));
        _innerMock
            .Setup(i => i.Execute<string>(commandMock.Object, target, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("ok"));
        var gateway = BuildGateway();

        // Act
        var result = await gateway.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_MaxRowsLimitWithNullPaging_PassesThroughToInner()
    {
        // Arrange — IQueryCommand with no Paging expression at all skips the check entirely.
        var connId = Guid.NewGuid();
        var limit = new TestConnectionLimitConfiguration { ConnectionConfigurationId = connId, MaxRows = 1 };
        var target = new DataStoreTarget("conn-null-paging", null, "Container");
        var queryCommand = BuildQueryCommandWithNullPaging();
        _resolverMock
            .Setup(r => r.Resolve("conn-null-paging", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration> { limit }));
        _innerMock
            .Setup(i => i.Execute<string>(queryCommand.Object, target, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("ok"));
        var gateway = BuildGateway();

        // Act
        var result = await gateway.Execute<string>(queryCommand.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_MaxRowsLimitWithUnboundedTake_TreatsNullTakeAsExceedingCap()
    {
        // Arrange — Paging present but Take is null ("fetch everything"): the production code treats
        // this as requesting int.MaxValue rows, which always exceeds any configured cap (fail-safe).
        var connId = Guid.NewGuid();
        var limit = new TestConnectionLimitConfiguration { ConnectionConfigurationId = connId, MaxRows = 50 };
        var target = new DataStoreTarget("conn-unbounded", null, "Container");
        var queryCommand = BuildQueryCommandWithPaging(take: null);
        _resolverMock
            .Setup(r => r.Resolve("conn-unbounded", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration> { limit }));
        var gateway = BuildGateway();

        // Act
        var result = await gateway.Execute<string>(queryCommand.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("ABSTRACTIONS6-81002");
    }

    // =========================================================================
    // Daily-budget trip (~:281-301)
    // =========================================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_DailyQueryBudgetExhausted_ReturnsDailyQueryBudgetExhaustedFailureWithoutCallingInner()
    {
        // Arrange
        var connId = Guid.NewGuid();
        _counters.IncrementQueryCount(connId);
        var limit = new TestConnectionLimitConfiguration { ConnectionConfigurationId = connId, MaxQueriesPerDay = 1 };
        var target = new DataStoreTarget("conn-query-budget", null, "Container");
        _resolverMock
            .Setup(r => r.Resolve("conn-query-budget", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration> { limit }));
        var gateway = BuildGateway();

        // Act
        var result = await gateway.Execute<string>(BuildCommand().Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("ABSTRACTIONS6-81004");
        _innerMock.Verify(
            i => i.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_DailyByteBudgetExhausted_ReturnsDailyByteBudgetExhaustedFailureWithoutCallingInner()
    {
        // Arrange
        var connId = Guid.NewGuid();
        _counters.IncrementByteCount(connId, 100);
        var limit = new TestConnectionLimitConfiguration { ConnectionConfigurationId = connId, MaxBytesPerDay = 100 };
        var target = new DataStoreTarget("conn-byte-budget", null, "Container");
        _resolverMock
            .Setup(r => r.Resolve("conn-byte-budget", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration> { limit }));
        var gateway = BuildGateway();

        // Act
        var result = await gateway.Execute<string>(BuildCommand().Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("ABSTRACTIONS6-81005");
        _innerMock.Verify(
            i => i.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_DailyBudgetWithinLimit_PassesThroughToInner()
    {
        // Arrange
        var connId = Guid.NewGuid();
        var limit = new TestConnectionLimitConfiguration { ConnectionConfigurationId = connId, MaxQueriesPerDay = 5 };
        var target = new DataStoreTarget("conn-budget-ok", null, "Container");
        _resolverMock
            .Setup(r => r.Resolve("conn-budget-ok", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration> { limit }));
        var commandMock = BuildCommand();
        _innerMock
            .Setup(i => i.Execute<string>(commandMock.Object, target, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("ok"));
        var gateway = BuildGateway();

        // Act
        var result = await gateway.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // =========================================================================
    // Timeout -> QueryTimeoutExceeded (~:155-164)
    // =========================================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_InnerExceedsConfiguredTimeout_ReturnsQueryTimeoutExceededFailure()
    {
        // Arrange — a zero-second timeout means the linked CancellationTokenSource cancels almost
        // immediately; the inner gateway's simulated long-running call observes the cancellation
        // and throws well before its own multi-second delay would otherwise elapse.
        var connId = Guid.NewGuid();
        var limit = new TestConnectionLimitConfiguration { ConnectionConfigurationId = connId, TimeoutSeconds = 0 };
        var target = new DataStoreTarget("conn-timeout", null, "Container");
        _resolverMock
            .Setup(r => r.Resolve("conn-timeout", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration> { limit }));
        _innerMock
            .Setup(i => i.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(async (IDataCommand _, DataStoreTarget _, bool _, CancellationToken ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
                return GenericResult<string>.Success("should never be reached");
            });
        var gateway = BuildGateway();

        // Act
        var result = await gateway.Execute<string>(BuildCommand().Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("ABSTRACTIONS6-81001");
    }

    // =========================================================================
    // Caller-token cancellation with NO timeout limit configured (~:167-176)
    // =========================================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_CallerCancelsDuringInnerExecuteWithNoTimeoutLimitConfigured_ReturnsCancelledResultWithoutThrowing()
    {
        // Why: regression guard for the second, unfiltered catch clause in ExecuteWithLimits
        // (LimitEnforcementDataGateway.cs ~167-176). With no EnforceTimeoutSeconds limit configured,
        // ApplyQueryTimeout hands back the CALLER's token unchanged (timeoutCts stays null), so the
        // FIRST catch clause's `timeoutCts?.IsCancellationRequested == true` filter can never match
        // the caller's own cancellation. Without the second clause the OperationCanceledException
        // would propagate uncaught, violating this gateway's documented "NEVER throws" contract —
        // it must come back as a cancelled IGenericResult instead.
        var connId = Guid.NewGuid();
        // Why: a limit row with no EnforceXxx properties set still makes limits.Count > 0, routing
        // execution through the main try/catch path instead of the early "no limits configured"
        // passthrough (which has no catch at all).
        var limit = new TestConnectionLimitConfiguration { ConnectionConfigurationId = connId };
        var target = new DataStoreTarget("conn-caller-cancel", null, "Container");
        _resolverMock
            .Setup(r => r.Resolve("conn-caller-cancel", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration> { limit }));
        using var cts = new CancellationTokenSource();
        _innerMock
            .Setup(i => i.Execute<string>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(async (IDataCommand _, DataStoreTarget _, bool _, CancellationToken ct) =>
            {
                await cts.CancelAsync().ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
                return GenericResult<string>.Success("should never be reached");
            });
        var gateway = BuildGateway();

        // Act
        var result = await gateway.Execute<string>(BuildCommand().Object, target, cts.Token);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("ABSTRACTIONS6-11006");
    }

    // =========================================================================
    // Success path increments daily counters (~:150)
    // =========================================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_SuccessWithDailyBudgetConfigured_IncrementsQueryCounter()
    {
        // Arrange
        var connId = Guid.NewGuid();
        var limit = new TestConnectionLimitConfiguration { ConnectionConfigurationId = connId, MaxQueriesPerDay = 100 };
        var target = new DataStoreTarget("conn-increment", null, "Container");
        var commandMock = BuildCommand();
        _resolverMock
            .Setup(r => r.Resolve("conn-increment", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration> { limit }));
        _innerMock
            .Setup(i => i.Execute<string>(commandMock.Object, target, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("ok"));
        var gateway = BuildGateway();

        // Act
        var result = await gateway.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var (queries, _) = _counters.Read(connId);
        queries.ShouldBe(1L);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_SuccessWithoutDailyBudgetConfigured_DoesNotIncrementCounter()
    {
        // Arrange — only a rate limit is configured (no MaxQueriesPerDay/MaxBytesPerDay), so the
        // daily counters must remain untouched on success.
        var connId = Guid.NewGuid();
        var limit = new TestConnectionLimitConfiguration { ConnectionConfigurationId = connId, RateLimit = 1000, BurstSize = 1000 };
        var target = new DataStoreTarget("conn-no-budget", null, "Container");
        var commandMock = BuildCommand();
        _resolverMock
            .Setup(r => r.Resolve("conn-no-budget", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration> { limit }));
        _innerMock
            .Setup(i => i.Execute<string>(commandMock.Object, target, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("ok"));
        var gateway = BuildGateway();

        // Act
        var result = await gateway.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var (queries, bytes) = _counters.Read(connId);
        queries.ShouldBe(0L);
        bytes.ShouldBe(0L);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_FailureWithDailyBudgetConfigured_DoesNotIncrementCounter()
    {
        // Arrange — the inner gateway fails; IncrementDailyCounters only runs on result.IsSuccess.
        var connId = Guid.NewGuid();
        var limit = new TestConnectionLimitConfiguration { ConnectionConfigurationId = connId, MaxQueriesPerDay = 100 };
        var target = new DataStoreTarget("conn-fail-no-increment", null, "Container");
        var commandMock = BuildCommand();
        _resolverMock
            .Setup(r => r.Resolve("conn-fail-no-increment", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration> { limit }));
        _innerMock
            .Setup(i => i.Execute<string>(commandMock.Object, target, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Failure(new GenericMessage("inner failed")));
        var gateway = BuildGateway();

        // Act
        var result = await gateway.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        var (queries, _) = _counters.Read(connId);
        queries.ShouldBe(0L);
    }

    // =========================================================================
    // useCache overload routing
    // =========================================================================

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_ThreeArgOverload_ForwardsUseCacheTrueToInner()
    {
        // Arrange
        var target = new DataStoreTarget("conn-cache-default", null, "Container");
        var commandMock = BuildCommand();
        _resolverMock
            .Setup(r => r.Resolve("conn-cache-default", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration>()));
        _innerMock
            .Setup(i => i.Execute<string>(commandMock.Object, target, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("ok"));
        var gateway = BuildGateway();

        // Act
        await gateway.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        _innerMock.Verify(i => i.Execute<string>(commandMock.Object, target, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_FourArgOverload_ForwardsUseCacheFalseToInner()
    {
        // Arrange
        var target = new DataStoreTarget("conn-cache-false", null, "Container");
        var commandMock = BuildCommand();
        _resolverMock
            .Setup(r => r.Resolve("conn-cache-false", It.IsAny<CancellationToken>()))
            .Returns(GenericResult<IReadOnlyList<ConnectionLimitConfiguration>>.Success(new List<ConnectionLimitConfiguration>()));
        _innerMock
            .Setup(i => i.Execute<string>(commandMock.Object, target, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("ok"));
        var gateway = BuildGateway();

        // Act
        await gateway.Execute<string>(commandMock.Object, target, useCache: false, TestContext.Current.CancellationToken);

        // Assert
        _innerMock.Verify(i => i.Execute<string>(commandMock.Object, target, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // DataSetTarget / BeginTransaction / OpenRecordSource bypass limit enforcement entirely
    // =========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task Execute_DataSetTarget_PassesThroughWithoutConsultingResolver()
    {
        // Arrange
        var target = new DataSetTarget("MyDataSet");
        var commandMock = BuildCommand();
        _innerMock
            .Setup(i => i.Execute<string>(commandMock.Object, target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<string>.Success("federated-value"));
        var gateway = BuildGateway();

        // Act
        var result = await gateway.Execute<string>(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("federated-value");
        _resolverMock.Verify(r => r.Resolve(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task BeginTransaction_DelegatesDirectlyToInner()
    {
        // Arrange
        var transactionMock = new Mock<IDataGatewayTransaction>();
        _innerMock
            .Setup(i => i.BeginTransaction("conn-txn", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataGatewayTransaction>.Success(transactionMock.Object));
        var gateway = BuildGateway();

        // Act
        var result = await gateway.BeginTransaction("conn-txn", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeSameAs(transactionMock.Object);
        _innerMock.Verify(i => i.BeginTransaction("conn-txn", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task OpenRecordSource_DelegatesDirectlyToInner()
    {
        // Arrange
        var recordSourceMock = new Mock<IRecordSource<DataRecord>>();
        var target = new DataStoreTarget("conn-record-source", null, "Container");
        var commandMock = BuildCommand();
        _innerMock
            .Setup(i => i.OpenRecordSource(commandMock.Object, target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRecordSource<DataRecord>>.Success(recordSourceMock.Object));
        var gateway = BuildGateway();

        // Act
        var result = await gateway.OpenRecordSource(commandMock.Object, target, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeSameAs(recordSourceMock.Object);
        _resolverMock.Verify(r => r.Resolve(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private LimitEnforcementDataGateway BuildGateway()
        => new(_innerMock.Object, _resolverMock.Object, _counters, NullLoggerFactory.Instance);

    private static Mock<IDataCommand> BuildCommand() => new();

    private static Mock<IQueryCommand> BuildQueryCommandWithPaging(int? take)
    {
        var pagingMock = new Mock<IPagingExpression>();
        pagingMock.Setup(p => p.Take).Returns(take);
        var commandMock = new Mock<IQueryCommand>();
        commandMock.Setup(c => c.Paging).Returns(pagingMock.Object);
        return commandMock;
    }

    private static Mock<IQueryCommand> BuildQueryCommandWithNullPaging()
    {
        var commandMock = new Mock<IQueryCommand>();
        commandMock.Setup(c => c.Paging).Returns((IPagingExpression?)null);
        return commandMock;
    }
}

/// <summary>
/// Test double exposing settable values for each <see cref="ConnectionLimitConfiguration"/>
/// virtual enforcement property. Production subclasses live in per-connection-type packages
/// (MsSql, Http) that this test project does not reference directly; this double exercises the
/// base class's documented override contract the same way they do.
/// </summary>
internal sealed class TestConnectionLimitConfiguration : ConnectionLimitConfiguration
{
    public TestConnectionLimitConfiguration() : base("Connection", "Test", "Test")
    {
    }

    public int? RateLimit { get; set; }

    public int? BurstSize { get; set; }

    public int? ConcurrencyLimit { get; set; }

    public int? MaxRows { get; set; }

    public int? TimeoutSeconds { get; set; }

    public int? MaxQueriesPerDay { get; set; }

    public long? MaxBytesPerDay { get; set; }

    public override int? EnforceMaxPerSecond => RateLimit;

    public override int? EnforceBurstSize => BurstSize;

    public override int? EnforceMaxConcurrent => ConcurrencyLimit;

    public override int? EnforceMaxRows => MaxRows;

    public override int? EnforceTimeoutSeconds => TimeoutSeconds;

    public override int? EnforceMaxQueriesPerDay => MaxQueriesPerDay;

    public override long? EnforceMaxBytesPerDay => MaxBytesPerDay;
}
