using System;
using System.Linq;
using Reference.Scheduler.Server.Endpoints;
using Reference.Scheduler.Server.Validators;
using Shouldly;
using Xunit;

namespace Reference.Scheduler.Server.Tests;

[Trait("Priority", "P1")]
[Trait("Category", "Scheduling")]
public sealed class UpdateScheduleRequestValidatorTests
{
    private readonly UpdateScheduleRequestValidator _sut = new();

    private static UpdateScheduleRequest CreateValidRequest() => new()
    {
        Name = "DailySync",
        PipelineName = "MyPipeline"
    };

    // Valid request

    [Fact]
    public void ValidateSucceedsWithValidRequest()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateSucceedsWithAllOptionalFieldsPopulated()
    {
        // Arrange
        var request = new UpdateScheduleRequest
        {
            Name = "DailySync",
            PipelineName = "MyPipeline",
            SchedulerType = "Cron",
            CronExpression = "0 0 * * *",
            IntervalSeconds = 300,
            TimeZoneId = "America/New_York",
            IsEnabled = false
        };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    // Name validation

    [Fact]
    public void ValidateFailsWhenNameIsEmpty()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Name = "";

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("Name is required", StringComparison.Ordinal));
    }

    // PipelineName validation

    [Fact]
    public void ValidateFailsWhenPipelineNameIsEmpty()
    {
        // Arrange
        var request = CreateValidRequest();
        request.PipelineName = "";

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("PipelineName is required", StringComparison.Ordinal));
    }

    // Multiple failures

    [Fact]
    public void ValidateReturnsMultipleErrorsWhenBothFieldsEmpty()
    {
        // Arrange
        var request = new UpdateScheduleRequest
        {
            Name = "",
            PipelineName = ""
        };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThanOrEqualTo(2);
    }
}
