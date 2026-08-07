using System;
using System.Reflection;
using System.Threading.Tasks;
using Fdw.Services.Data.Limits;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Tests for <see cref="DailyLimitResetJob"/> — the hosted service that resets in-memory daily
/// limit counters at midnight UTC.
/// </summary>
/// <remarks>
/// The real reset only fires once per day at the midnight-UTC boundary, which a unit test cannot
/// wait for. <see cref="ResetCounters_ClearsAllTrackedConnectionsAndReturnsCount"/> and
/// <see cref="ComputeDelayUntilMidnightUtc_ReturnsPositiveDelayWithinOneDay"/> invoke the private
/// reset/delay logic directly via reflection (an established pattern in this repository for
/// otherwise-untestable time-gated internals) to verify it without waiting for the actual boundary.
/// <see cref="StartAsync_ThenStopAsync_ExitsCleanlyWithoutResettingCounters"/> instead exercises the
/// public <see cref="BackgroundService"/> lifecycle and its clean-shutdown cancellation path.
/// </remarks>
[Collection(nameof(DataServiceTestCollection))]
public sealed class DailyLimitResetJobTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_NullCounters_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new DailyLimitResetJob(null!, NullLoggerFactory.Instance));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_NullLoggerFactory_FallsBackToNullLoggerFactoryAndDoesNotThrow()
    {
        // Why: `loggerFactory ?? NullLoggerFactory.Instance` is the only sanctioned `??` fallback
        // pattern in this codebase — it must not throw.
        var counters = new ConnectionLimitCounterStore();

        Should.NotThrow(() => new DailyLimitResetJob(counters, null));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task StartAsync_ThenStopAsync_ExitsCleanlyWithoutResettingCounters()
    {
        // Arrange — the midnight-UTC delay is ~24h in the worst case, so starting then immediately
        // stopping exercises the OperationCanceledException catch-and-clean-return shutdown branch,
        // never the actual reset branch (which only fires at the boundary).
        var counters = new ConnectionLimitCounterStore();
        var connectionId = Guid.NewGuid();
        counters.IncrementQueryCount(connectionId);
        var job = new DailyLimitResetJob(counters, NullLoggerFactory.Instance);

        // Act
        await job.StartAsync(TestContext.Current.CancellationToken);
        await job.StopAsync(TestContext.Current.CancellationToken);

        // Assert — the job never reached ResetCounters(), so the seeded counter is untouched.
        var (queries, _) = counters.Read(connectionId);
        queries.ShouldBe(1L);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ResetCounters_ClearsAllTrackedConnectionsAndReturnsTrackedConnectionCount()
    {
        // Arrange
        var counters = new ConnectionLimitCounterStore();
        var connectionIdA = Guid.NewGuid();
        var connectionIdB = Guid.NewGuid();
        counters.IncrementQueryCount(connectionIdA);
        counters.IncrementQueryCount(connectionIdB);
        counters.IncrementByteCount(connectionIdB, 500);
        var job = new DailyLimitResetJob(counters, NullLoggerFactory.Instance);
        var method = typeof(DailyLimitResetJob).GetMethod("ResetCounters", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var resetCount = (int)method.Invoke(job, null)!;

        // Assert
        resetCount.ShouldBe(2);
        var (queriesA, bytesA) = counters.Read(connectionIdA);
        var (queriesB, bytesB) = counters.Read(connectionIdB);
        queriesA.ShouldBe(0L);
        bytesA.ShouldBe(0L);
        queriesB.ShouldBe(0L);
        bytesB.ShouldBe(0L);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ResetCounters_NoTrackedConnections_ReturnsZero()
    {
        // Arrange
        var counters = new ConnectionLimitCounterStore();
        var job = new DailyLimitResetJob(counters, NullLoggerFactory.Instance);
        var method = typeof(DailyLimitResetJob).GetMethod("ResetCounters", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var resetCount = (int)method.Invoke(job, null)!;

        // Assert
        resetCount.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "DataIntegrity")]
    public void ComputeDelayUntilMidnightUtc_ReturnsPositiveDelayWithinOneDay()
    {
        // Arrange
        var method = typeof(DailyLimitResetJob).GetMethod(
            "ComputeDelayUntilMidnightUtc", BindingFlags.NonPublic | BindingFlags.Static)!;

        // Act
        var delay = (TimeSpan)method.Invoke(null, null)!;

        // Assert — guarded to be strictly positive (never zero/negative from clock skew) and
        // bounded by the maximum possible distance to the next midnight UTC.
        delay.ShouldBeGreaterThan(TimeSpan.Zero);
        delay.ShouldBeLessThanOrEqualTo(TimeSpan.FromDays(1));
    }
}
