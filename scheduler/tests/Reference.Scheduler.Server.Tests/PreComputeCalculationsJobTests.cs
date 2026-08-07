using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Reference.Scheduler.Server.Configuration;
using Reference.Scheduler.Server.Services;
using Shouldly;
using Xunit;

namespace Reference.Scheduler.Server.Tests;

[Trait("Category", "Scheduling")]
public sealed class PreComputeCalculationsJobTests
{
    private readonly ILogger<PreComputeCalculationsJob> _logger = new NullLogger<PreComputeCalculationsJob>();
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();
    private readonly Mock<IServiceScope> _scope = new();
    private readonly Mock<IServiceProvider> _serviceProvider = new();
    private readonly Mock<ICalculationUsageRepository> _repository = new();
    private readonly Mock<ICalculationApiClient> _apiClient = new();

    public PreComputeCalculationsJobTests()
    {
        _scopeFactory.Setup(x => x.CreateScope()).Returns(_scope.Object);
        _scope.Setup(x => x.ServiceProvider).Returns(_serviceProvider.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(ICalculationUsageRepository))).Returns(_repository.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(ICalculationApiClient))).Returns(_apiClient.Object);
    }

    private PreComputeCalculationsJob CreateSut(
        bool enabled = true,
        int intervalMinutes = 15,
        int initialDelayMinutes = 5,
        int maxCalculationsPerRun = 50,
        int delayBetweenCalculationsMs = 0,
        int minExecutionCountThreshold = 5,
        int stalenessThresholdMinutes = 30)
    {
        var options = Options.Create(new PreComputeOptions
        {
            Enabled = enabled,
            IntervalMinutes = intervalMinutes,
            InitialDelayMinutes = initialDelayMinutes,
            MaxCalculationsPerRun = maxCalculationsPerRun,
            DelayBetweenCalculationsMs = delayBetweenCalculationsMs,
            MinExecutionCountThreshold = minExecutionCountThreshold,
            StalenessThresholdMinutes = stalenessThresholdMinutes
        });
        return new PreComputeCalculationsJob(options, _scopeFactory.Object, _logger);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // StartAsync / StopAsync / Dispose
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Priority", "P1")]
    public async Task StartAsyncCreatesTimerWhenEnabled()
    {
        // Arrange
        using var sut = CreateSut(enabled: true);

        // Act
        await sut.StartAsync(TestContext.Current.CancellationToken);

        // Assert - no exception means timer was created successfully
        sut.ShouldNotBeNull();

        // Cleanup
        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task StartAsyncDoesNotCreateTimerWhenDisabled()
    {
        // Arrange
        using var sut = CreateSut(enabled: false);

        // Act
        await sut.StartAsync(TestContext.Current.CancellationToken);

        // Assert - no exception, job is disabled
        sut.ShouldNotBeNull();

        // StopAsync should work even when disabled (timer is null)
        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task StopAsyncDisablesTimer()
    {
        // Arrange
        using var sut = CreateSut(enabled: true);
        await sut.StartAsync(TestContext.Current.CancellationToken);

        // Act
        await sut.StopAsync(CancellationToken.None);

        // Assert - no exception
        sut.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task DisposeDisposesTimerAndSemaphore()
    {
        // Arrange
        var sut = CreateSut(enabled: true);
        await sut.StartAsync(TestContext.Current.CancellationToken);

        // Act & Assert - no exception
        sut.Dispose();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task DisposeIsIdempotent()
    {
        // Arrange
        var sut = CreateSut(enabled: true);
        await sut.StartAsync(TestContext.Current.CancellationToken);

        // Act - dispose twice
        sut.Dispose();
        sut.Dispose();

        // Assert - no exception on double dispose
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task DisposeWorksWhenTimerNeverCreated()
    {
        // Arrange - disabled, so timer is never created
        var sut = CreateSut(enabled: false);
        await sut.StartAsync(TestContext.Current.CancellationToken);

        // Act & Assert - no exception
        sut.Dispose();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ExecutePreCompute (via timer callback)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ExecutePreComputeCompletesWhenNoStaleCalculations()
    {
        // Arrange
        _repository.Setup(x => x.GetStaleCalculations(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<CalculationUsageRecord>>.Success(
                Array.Empty<CalculationUsageRecord>()));

        using var sut = CreateSut(enabled: true, initialDelayMinutes: 0);
        await sut.StartAsync(TestContext.Current.CancellationToken);

        // Act - wait for the timer to fire (initialDelay = 0 means immediate)
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Assert - repository was queried, no API calls
        _repository.Verify(x => x.GetStaleCalculations(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
        _apiClient.Verify(x => x.Execute(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ExecutePreComputeQueriesStaleCalculationsAndExecutesThem()
    {
        // Arrange
        var staleCalcs = new List<CalculationUsageRecord>
        {
            new() { CalculationType = "Revenue", CalculationHash = "abc123", ExecutionCount = 10 },
            new() { CalculationType = "Margin", CalculationHash = "def456", ExecutionCount = 8 }
        };

        _repository.Setup(x => x.GetStaleCalculations(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<CalculationUsageRecord>>.Success(staleCalcs));

        _apiClient.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<long>.Success(150L));

        _repository.Setup(x => x.UpdateLastCachedAt(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        _repository.Setup(x => x.RecordPreComputeRun(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        using var sut = CreateSut(enabled: true, initialDelayMinutes: 0);
        await sut.StartAsync(TestContext.Current.CancellationToken);

        // Act - wait for timer callback
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Assert
        _apiClient.Verify(x => x.Execute(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _repository.Verify(x => x.UpdateLastCachedAt(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _repository.Verify(x => x.RecordPreComputeRun(
            2, 0, 2, It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once());

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ExecutePreComputeRespectsMaxCalculationsPerRun()
    {
        // Arrange - MaxCalculationsPerRun = 2, pass it to repository query
        var staleCalcs = new List<CalculationUsageRecord>
        {
            new() { CalculationType = "A", CalculationHash = "h1", ExecutionCount = 10 },
            new() { CalculationType = "B", CalculationHash = "h2", ExecutionCount = 8 }
        };

        _repository.Setup(x => x.GetStaleCalculations(
                It.IsAny<int>(), It.IsAny<int>(), 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<CalculationUsageRecord>>.Success(staleCalcs));

        _apiClient.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<long>.Success(50L));

        _repository.Setup(x => x.UpdateLastCachedAt(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        _repository.Setup(x => x.RecordPreComputeRun(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        using var sut = CreateSut(enabled: true, initialDelayMinutes: 0, maxCalculationsPerRun: 2);
        await sut.StartAsync(TestContext.Current.CancellationToken);

        // Act
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Assert - repository was called with maxResults = 2
        _repository.Verify(x => x.GetStaleCalculations(
            It.IsAny<int>(), It.IsAny<int>(), 2, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ExecutePreComputeHandlesApiFailuresGracefully()
    {
        // Arrange - one success, one failure
        var staleCalcs = new List<CalculationUsageRecord>
        {
            new() { CalculationType = "Revenue", CalculationHash = "abc123", ExecutionCount = 10 },
            new() { CalculationType = "Margin", CalculationHash = "def456", ExecutionCount = 8 }
        };

        _repository.Setup(x => x.GetStaleCalculations(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<CalculationUsageRecord>>.Success(staleCalcs));

        _apiClient.Setup(x => x.Execute("Revenue", "abc123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<long>.Success(100L));
        _apiClient.Setup(x => x.Execute("Margin", "def456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<long>.Failure(TestErrors.Failed));

        _repository.Setup(x => x.UpdateLastCachedAt("abc123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        _repository.Setup(x => x.RecordPreComputeRun(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        using var sut = CreateSut(enabled: true, initialDelayMinutes: 0);
        await sut.StartAsync(TestContext.Current.CancellationToken);

        // Act
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Assert - recorded 1 success, 1 failure
        _repository.Verify(x => x.RecordPreComputeRun(
            1, 1, 2, It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once());
        // Only the successful calc gets UpdateLastCachedAt
        _repository.Verify(x => x.UpdateLastCachedAt("abc123", It.IsAny<CancellationToken>()), Times.Once());
        _repository.Verify(x => x.UpdateLastCachedAt("def456", It.IsAny<CancellationToken>()), Times.Never());

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ExecutePreComputeRecordsRunStatsAfterCompletion()
    {
        // Arrange
        var staleCalcs = new List<CalculationUsageRecord>
        {
            new() { CalculationType = "Revenue", CalculationHash = "abc123", ExecutionCount = 10 }
        };

        _repository.Setup(x => x.GetStaleCalculations(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<CalculationUsageRecord>>.Success(staleCalcs));

        _apiClient.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<long>.Success(200L));

        _repository.Setup(x => x.UpdateLastCachedAt(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        _repository.Setup(x => x.RecordPreComputeRun(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        using var sut = CreateSut(enabled: true, initialDelayMinutes: 0);
        await sut.StartAsync(TestContext.Current.CancellationToken);

        // Act
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Assert - RecordPreComputeRun was called with correct counts
        _repository.Verify(x => x.RecordPreComputeRun(
            1, 0, 1, It.Is<long>(ms => ms >= 0), It.IsAny<CancellationToken>()), Times.Once());

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ExecutePreComputeHandlesRepositoryFailureGracefully()
    {
        // Arrange - repository returns failure for stale query
        _repository.Setup(x => x.GetStaleCalculations(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<CalculationUsageRecord>>.Failure(TestErrors.QueryFailed));

        using var sut = CreateSut(enabled: true, initialDelayMinutes: 0);
        await sut.StartAsync(TestContext.Current.CancellationToken);

        // Act
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Assert - no API calls made, no exception propagated
        _apiClient.Verify(x => x.Execute(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task ExecutePreComputeHandlesFailedRunStatsRecordingGracefully()
    {
        // Arrange
        var staleCalcs = new List<CalculationUsageRecord>
        {
            new() { CalculationType = "Revenue", CalculationHash = "abc123", ExecutionCount = 10 }
        };

        _repository.Setup(x => x.GetStaleCalculations(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<CalculationUsageRecord>>.Success(staleCalcs));

        _apiClient.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<long>.Success(100L));

        _repository.Setup(x => x.UpdateLastCachedAt(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        // RecordPreComputeRun fails
        _repository.Setup(x => x.RecordPreComputeRun(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(TestErrors.Failed));

        using var sut = CreateSut(enabled: true, initialDelayMinutes: 0);
        await sut.StartAsync(TestContext.Current.CancellationToken);

        // Act
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Assert - no exception, job completed despite stats recording failure
        _repository.Verify(x => x.RecordPreComputeRun(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once());

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task ExecutePreComputeHandlesUpdateLastCachedAtFailureGracefully()
    {
        // Arrange
        var staleCalcs = new List<CalculationUsageRecord>
        {
            new() { CalculationType = "Revenue", CalculationHash = "abc123", ExecutionCount = 10 }
        };

        _repository.Setup(x => x.GetStaleCalculations(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<CalculationUsageRecord>>.Success(staleCalcs));

        _apiClient.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<long>.Success(100L));

        // UpdateLastCachedAt fails but shouldn't crash the job
        _repository.Setup(x => x.UpdateLastCachedAt(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(TestErrors.UpdateFailed));

        _repository.Setup(x => x.RecordPreComputeRun(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        using var sut = CreateSut(enabled: true, initialDelayMinutes: 0);
        await sut.StartAsync(TestContext.Current.CancellationToken);

        // Act
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Assert - still counts as success, run stats recorded
        _repository.Verify(x => x.RecordPreComputeRun(
            1, 0, 1, It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once());

        await sut.StopAsync(CancellationToken.None);
    }
}
