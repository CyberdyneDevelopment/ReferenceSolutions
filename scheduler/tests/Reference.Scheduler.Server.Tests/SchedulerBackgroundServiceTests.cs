using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Reference.Scheduler.Server.Services;
using Shouldly;
using Xunit;
using LocalConfig = Reference.Scheduler.Server.Configuration.SchedulerConfiguration;
// Why: FDW SchedulerConfiguration and local SchedulerConfiguration share the same short name;
// alias the FDW type to avoid ambiguity in test setups.
using FdwSchedulerConfig = Fdw.Services.Scheduling.SchedulerConfiguration;
using FdwScheduleConfig = Fdw.Services.Scheduling.Abstractions.Configuration.ScheduleConfiguration;

namespace Reference.Scheduler.Server.Tests;

[Trait("Priority", "P1")]
[Trait("Category", "Scheduling")]
public sealed class SchedulerBackgroundServiceTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();
    private readonly Mock<IServiceScope> _scope = new();
    private readonly Mock<IServiceProvider> _serviceProvider = new();
    // Why: IDataGateway is now resolved directly from scope for timestamp update writes;
    // IFrameworkSchedulingService.Execute(IGenericCommand) was removed in FDW 1.5.0.
    private readonly Mock<IDataGateway> _dataGateway = new();
    private readonly Mock<IEtlDispatchService> _dispatchService = new();
    // Why: IServiceConfigurationProvider<T> is what the background service resolves from scope
    // after the refactor from IOptionsMonitor<List<SchedulerConfiguration>> + ScheduleQueryRecord.
    private readonly Mock<IServiceConfigurationProvider<FdwSchedulerConfig>> _schedulerConfigProvider = new();
    private readonly Mock<IServiceConfigurationProvider<FdwScheduleConfig>> _scheduleConfigProvider = new();
    private readonly ILogger<SchedulerBackgroundService> _logger = new NullLogger<SchedulerBackgroundService>();

    private static readonly FdwSchedulerConfig FdwConfig = new()
    {
        Name = "TestScheduler",
        DataStoreName = "TestStore",
        PathName = "sched",
        ScheduleContainerName = "Schedule"
    };

    public SchedulerBackgroundServiceTests()
    {
        _scopeFactory.Setup(x => x.CreateScope()).Returns(_scope.Object);
        _scope.Setup(x => x.ServiceProvider).Returns(_serviceProvider.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(IDataGateway))).Returns(_dataGateway.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(IEtlDispatchService))).Returns(_dispatchService.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(IServiceConfigurationProvider<FdwSchedulerConfig>)))
            .Returns(_schedulerConfigProvider.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(IServiceConfigurationProvider<FdwScheduleConfig>)))
            .Returns(_scheduleConfigProvider.Object);

        // Default scheduler config provider returns the single test scheduler record.
        _schedulerConfigProvider
            .Setup(x => x.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<FdwSchedulerConfig>>.Success(new[] { FdwConfig }));
    }

    private SchedulerBackgroundService CreateSut(int evaluationIntervalSeconds = 1)
    {
        var options = Options.Create(new LocalConfig
        {
            EvaluationIntervalSeconds = evaluationIntervalSeconds
        });
        return new SchedulerBackgroundService(_scopeFactory.Object, _logger, options);
    }

    private static FdwScheduleConfig CreateScheduleConfig(
        string name = "TestSchedule",
        string pipelineName = "TestPipeline",
        string serviceOptionType = "Cron",
        string? cronExpression = "* * * * *",
        int? intervalSeconds = null,
        bool isEnabled = true,
        DateTimeOffset? lastRunTime = null,
        DateTimeOffset? nextRunTime = null,
        Guid? tenantId = null) => new(serviceOptionType, serviceOptionType, "Schedules")
    {
        Name = name,
        PipelineName = pipelineName,
        CronExpression = cronExpression,
        IntervalSeconds = intervalSeconds,
        TimeZoneId = "UTC",
        IsEnabled = isEnabled,
        LastRunTime = lastRunTime,
        NextRunTime = nextRunTime,
        TenantId = tenantId
    };

    private void SetupScheduleListReturns(IReadOnlyList<FdwScheduleConfig>? records)
    {
        if (records == null)
        {
            _scheduleConfigProvider
                .Setup(x => x.Get(It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IReadOnlyList<FdwScheduleConfig>>.Success(null!));
        }
        else
        {
            _scheduleConfigProvider
                .Setup(x => x.Get(It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IReadOnlyList<FdwScheduleConfig>>.Success(records));
        }
    }

    private void SetupUpdateReturns(bool success = true)
    {
        // Why: IDataGateway.Execute<T>(IDataCommand, DataStoreTarget, ct) is the interface method
        // called by the DataGatewayCallExtensions.Execute<T>(IDataGateway, DataGatewayCall, ct) extension.
        // The extension cannot be mocked; mock the underlying interface method instead.
        _dataGateway.Setup(x => x.Execute<int>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success
                ? GenericResult<int>.Success(1)
                : GenericResult<int>.Failure(TestErrors.UpdateFailed));
    }

    // ExecuteAsync tests

    [Fact]
    public async Task ExecuteAsyncCallsEvaluateSchedulesAndLoops()
    {
        // Arrange
        SetupScheduleListReturns(Array.Empty<FdwScheduleConfig>());

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var sut = CreateSut(evaluationIntervalSeconds: 1);

        // Act
        await sut.StartAsync(cts.Token);
        try { await Task.Delay(300, cts.Token); } catch (OperationCanceledException) { }
        await sut.StopAsync(CancellationToken.None);

        // Assert - schedule config provider was queried at least once
        _scheduleConfigProvider.Verify(x => x.Get(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task ExecuteAsyncStopsWhenCancellationRequested()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        SetupScheduleListReturns(Array.Empty<FdwScheduleConfig>());

        var sut = CreateSut();

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        await sut.StopAsync(CancellationToken.None);

        // Assert - should have exited gracefully
        sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task ExecuteAsyncContinuesAfterEvaluationException()
    {
        // Arrange - throw on first call, succeed on second
        var callCount = 0;
        _scheduleConfigProvider
            .Setup(x => x.Get(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken _) =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("Database error");
                return Task.FromResult<IGenericResult<IReadOnlyList<FdwScheduleConfig>>>(
                    GenericResult<IReadOnlyList<FdwScheduleConfig>>.Success(
                        Array.Empty<FdwScheduleConfig>()));
            });

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        var sut = CreateSut(evaluationIntervalSeconds: 0);

        // Act
        await sut.StartAsync(cts.Token);
        try { await Task.Delay(600, cts.Token); } catch (OperationCanceledException) { }
        await sut.StopAsync(CancellationToken.None);

        // Assert - should have been called more than once (loop continued after error)
        callCount.ShouldBeGreaterThan(1);
    }

    // EvaluateSchedules tests (tested indirectly via ExecuteAsync)

    [Fact]
    public async Task EvaluateSchedulesReturnsEarlyWhenQueryFails()
    {
        // Arrange
        _scheduleConfigProvider
            .Setup(x => x.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<FdwScheduleConfig>>.Failure(TestErrors.Failed));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var sut = CreateSut(evaluationIntervalSeconds: 1);

        // Act
        await sut.StartAsync(cts.Token);
        try { await Task.Delay(300, cts.Token); } catch (OperationCanceledException) { }
        await sut.StopAsync(CancellationToken.None);

        // Assert - dispatch was never called
        _dispatchService.Verify(x => x.Dispatch(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task EvaluateSchedulesSkipsDisabledSchedules()
    {
        // Arrange
        SetupScheduleListReturns([CreateScheduleConfig(isEnabled: false)]);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var sut = CreateSut(evaluationIntervalSeconds: 1);

        // Act
        await sut.StartAsync(cts.Token);
        try { await Task.Delay(300, cts.Token); } catch (OperationCanceledException) { }
        await sut.StopAsync(CancellationToken.None);

        // Assert - dispatch was never called for disabled schedule
        _dispatchService.Verify(x => x.Dispatch(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task EvaluateSchedulesSkipsUnknownSchedulerType()
    {
        // Arrange
        SetupScheduleListReturns([CreateScheduleConfig(serviceOptionType: "Unknown")]);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var sut = CreateSut(evaluationIntervalSeconds: 1);

        // Act
        await sut.StartAsync(cts.Token);
        try { await Task.Delay(300, cts.Token); } catch (OperationCanceledException) { }
        await sut.StopAsync(CancellationToken.None);

        // Assert - dispatch was never called for unknown type
        _dispatchService.Verify(x => x.Dispatch(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task EvaluateSchedulesDispatchesDueSchedule()
    {
        // Arrange - interval schedule, last run was 2 hours ago (clearly due)
        SetupScheduleListReturns([CreateScheduleConfig(
            serviceOptionType: "Interval",
            cronExpression: null,
            intervalSeconds: 60,
            lastRunTime: DateTimeOffset.UtcNow.AddHours(-2))]);
        SetupUpdateReturns();
        _dispatchService.Setup(x => x.Dispatch(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var sut = CreateSut(evaluationIntervalSeconds: 10);

        // Act
        await sut.StartAsync(cts.Token);
        try { await Task.Delay(300, cts.Token); } catch (OperationCanceledException) { }
        await sut.StopAsync(CancellationToken.None);

        // Assert
        _dispatchService.Verify(x => x.Dispatch(
            "TestSchedule", "TestPipeline", "Scheduled", It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task EvaluateSchedulesDispatchesWithScheduleTenantId()
    {
        // Arrange
        // Why: this is the tenant-propagation half of the background-execution isolation seam — the
        // schedule's own TenantId must ride on the dispatch call so the ETL server's dispatched
        // execution's RLS SESSION_CONTEXT is scoped correctly (see EtlDispatchService/UnifiedTriggerEndpoint).
        var tenantId = Guid.NewGuid();
        SetupScheduleListReturns([CreateScheduleConfig(
            serviceOptionType: "Interval",
            cronExpression: null,
            intervalSeconds: 60,
            lastRunTime: DateTimeOffset.UtcNow.AddHours(-2),
            tenantId: tenantId)]);
        SetupUpdateReturns();
        _dispatchService.Setup(x => x.Dispatch(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var sut = CreateSut(evaluationIntervalSeconds: 10);

        // Act
        await sut.StartAsync(cts.Token);
        try { await Task.Delay(300, cts.Token); } catch (OperationCanceledException) { }
        await sut.StopAsync(CancellationToken.None);

        // Assert
        _dispatchService.Verify(x => x.Dispatch(
            "TestSchedule", "TestPipeline", "Scheduled", tenantId, It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task EvaluateSchedulesUpdatesTimestampsAfterDispatch()
    {
        // Arrange - interval schedule, last run was 2 hours ago (clearly due)
        SetupScheduleListReturns([CreateScheduleConfig(
            serviceOptionType: "Interval",
            cronExpression: null,
            intervalSeconds: 60,
            lastRunTime: DateTimeOffset.UtcNow.AddHours(-2))]);
        SetupUpdateReturns();
        _dispatchService.Setup(x => x.Dispatch(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var sut = CreateSut(evaluationIntervalSeconds: 10);

        // Act
        await sut.StartAsync(cts.Token);
        try { await Task.Delay(300, cts.Token); } catch (OperationCanceledException) { }
        await sut.StopAsync(CancellationToken.None);

        // Assert - IDataGateway.Execute<int> was called for the timestamp update
        _dataGateway.Verify(x => x.Execute<int>(
            It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task EvaluateSchedulesRevertsOnDispatchFailure()
    {
        // Arrange - interval schedule, last run was 2 hours ago (clearly due)
        SetupScheduleListReturns([CreateScheduleConfig(
            serviceOptionType: "Interval",
            cronExpression: null,
            intervalSeconds: 60,
            lastRunTime: DateTimeOffset.UtcNow.AddHours(-2))]);
        SetupUpdateReturns();
        _dispatchService.Setup(x => x.Dispatch(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(TestErrors.DispatchFailed));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var sut = CreateSut(evaluationIntervalSeconds: 10);

        // Act
        await sut.StartAsync(cts.Token);
        try { await Task.Delay(300, cts.Token); } catch (OperationCanceledException) { }
        await sut.StopAsync(CancellationToken.None);

        // Assert - IDataGateway.Execute<int> was called at least twice: pre-update and revert
        _dataGateway.Verify(x => x.Execute<int>(
            It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task EvaluateSchedulesDoesNotDispatchManualType()
    {
        // Arrange - Manual type always returns false from IsDue
        SetupScheduleListReturns([CreateScheduleConfig(serviceOptionType: "Manual", cronExpression: null)]);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var sut = CreateSut(evaluationIntervalSeconds: 10);

        // Act
        await sut.StartAsync(cts.Token);
        try { await Task.Delay(300, cts.Token); } catch (OperationCanceledException) { }
        await sut.StopAsync(CancellationToken.None);

        // Assert - Manual type never dispatches
        _dispatchService.Verify(x => x.Dispatch(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task EvaluateSchedulesDoesNotDispatchNonDueSchedule()
    {
        // Arrange - yearly cron, just ran
        SetupScheduleListReturns([CreateScheduleConfig(
            lastRunTime: DateTimeOffset.UtcNow,
            cronExpression: "0 0 1 1 *")]);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var sut = CreateSut(evaluationIntervalSeconds: 10);

        // Act
        await sut.StartAsync(cts.Token);
        try { await Task.Delay(300, cts.Token); } catch (OperationCanceledException) { }
        await sut.StopAsync(CancellationToken.None);

        // Assert - not due, no dispatch
        _dispatchService.Verify(x => x.Dispatch(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task EvaluateSchedulesReturnsEarlyWhenScheduleListIsNull()
    {
        // Arrange - success but null value
        SetupScheduleListReturns(null);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var sut = CreateSut(evaluationIntervalSeconds: 10);

        // Act
        await sut.StartAsync(cts.Token);
        try { await Task.Delay(300, cts.Token); } catch (OperationCanceledException) { }
        await sut.StopAsync(CancellationToken.None);

        // Assert - no dispatch
        _dispatchService.Verify(x => x.Dispatch(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never());
    }
}
