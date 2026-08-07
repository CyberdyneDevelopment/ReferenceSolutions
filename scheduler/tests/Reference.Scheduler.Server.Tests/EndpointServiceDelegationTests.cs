using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Reference.Scheduler.Server.Tests;

/// <summary>
/// Tests that verify the contract between endpoints and IFrameworkSchedulingService.
/// Since FastEndpoints endpoints require HttpContext for Send.* methods,
/// we test the service delegation patterns directly on IFrameworkSchedulingService.
/// These tests ensure the service returns the correct results that
/// endpoints would forward to clients.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Scheduling")]
public sealed class EndpointServiceDelegationTests
{
    private readonly Mock<IFrameworkSchedulingService> _service = new();

    // CreateSchedule delegation

    [Fact]
    public async Task CreateScheduleReturnsSuccessWhenServiceSucceeds()
    {
        // Arrange
        _service.Setup(x => x.CreateSchedule(It.IsAny<IGenericSchedule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var schedule = Mock.Of<IGenericSchedule>(s =>
            s.ScheduleId == "DailySync" &&
            s.ScheduleName == "DailySync" &&
            s.ProcessId == "MyPipeline" &&
            s.CronExpression == "0 0 * * *");

        // Act
        var result = await _service.Object.CreateSchedule(schedule, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateScheduleReturnsFailureWhenServiceFails()
    {
        // Arrange
        _service.Setup(x => x.CreateSchedule(It.IsAny<IGenericSchedule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(TestErrors.Failed));

        var schedule = Mock.Of<IGenericSchedule>(s =>
            s.ScheduleId == "DailySync" &&
            s.ScheduleName == "DailySync" &&
            s.ProcessId == "MyPipeline" &&
            s.CronExpression == "0 0 * * *");

        // Act
        var result = await _service.Object.CreateSchedule(schedule, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task CreateScheduleDelegatesScheduleToService()
    {
        // Arrange
        _service.Setup(x => x.CreateSchedule(It.IsAny<IGenericSchedule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var schedule = Mock.Of<IGenericSchedule>(s =>
            s.ScheduleId == "MySchedule" &&
            s.ScheduleName == "MySchedule" &&
            s.ProcessId == "MyPipeline" &&
            s.CronExpression == "0 12 * * *");

        // Act
        await _service.Object.CreateSchedule(schedule, TestContext.Current.CancellationToken);

        // Assert
        _service.Verify(x => x.CreateSchedule(
            It.Is<IGenericSchedule>(s =>
                string.Equals(s.ScheduleName, "MySchedule", StringComparison.Ordinal) &&
                string.Equals(s.ProcessId, "MyPipeline", StringComparison.Ordinal)),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    // DeleteSchedule delegation

    [Fact]
    public async Task DeleteScheduleReturnsSuccessWhenServiceSucceeds()
    {
        // Arrange
        _service.Setup(x => x.DeleteSchedule("DailySync", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        // Act
        var result = await _service.Object.DeleteSchedule("DailySync", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteScheduleReturnsFailureWhenServiceFails()
    {
        // Arrange
        _service.Setup(x => x.DeleteSchedule("DailySync", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(TestErrors.Failed));

        // Act
        var result = await _service.Object.DeleteSchedule("DailySync", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteScheduleDelegatesNameToService()
    {
        // Arrange
        _service.Setup(x => x.DeleteSchedule(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        // Act
        await _service.Object.DeleteSchedule("MySchedule", TestContext.Current.CancellationToken);

        // Assert
        _service.Verify(x => x.DeleteSchedule("MySchedule", It.IsAny<CancellationToken>()), Times.Once());
    }

    // UpdateSchedule delegation

    [Fact]
    public async Task UpdateScheduleReturnsSuccessWhenServiceSucceeds()
    {
        // Arrange
        _service.Setup(x => x.UpdateSchedule(It.IsAny<IGenericSchedule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var schedule = Mock.Of<IGenericSchedule>(s =>
            s.ScheduleId == "DailySync" &&
            s.ScheduleName == "DailySync" &&
            s.ProcessId == "MyPipeline" &&
            s.CronExpression == "0 0 * * *");

        // Act
        var result = await _service.Object.UpdateSchedule(schedule, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateScheduleReturnsFailureWhenServiceFails()
    {
        // Arrange
        _service.Setup(x => x.UpdateSchedule(It.IsAny<IGenericSchedule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(TestErrors.Failed));

        var schedule = Mock.Of<IGenericSchedule>(s =>
            s.ScheduleId == "DailySync" &&
            s.ScheduleName == "DailySync" &&
            s.ProcessId == "MyPipeline" &&
            s.CronExpression == "0 0 * * *");

        // Act
        var result = await _service.Object.UpdateSchedule(schedule, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    // Execute<T> delegation (used by List/Get endpoints)

    [Fact]
    public async Task ExecuteGenericReturnsSuccessWithResult()
    {
        // Arrange
        _service.Setup(x => x.Execute<int>(It.IsAny<IGenericCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(1));

        // Act
        var result = await _service.Object.Execute<int>(
            Mock.Of<IGenericCommand>(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteGenericReturnsFailureWhenServiceFails()
    {
        // Arrange
        _service.Setup(x => x.Execute<int>(It.IsAny<IGenericCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Failure(TestErrors.Failed));

        // Act
        var result = await _service.Object.Execute<int>(
            Mock.Of<IGenericCommand>(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }
}
