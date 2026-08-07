using System;
using System.Linq;
using Reference.Scheduler.Server.Endpoints;
using Reference.Scheduler.Server.Validators;
using Shouldly;
using Xunit;

namespace Reference.Scheduler.Server.Tests;

[Trait("Priority", "P1")]
[Trait("Category", "Scheduling")]
public sealed class CreateScheduleRequestValidatorTests
{
    private readonly CreateScheduleRequestValidator _sut = new();

    private static CreateScheduleRequest CreateValidRequest() => new()
    {
        Name = "DailySync",
        PipelineName = "MyPipeline",
        CronExpression = "0 0 * * *"
    };

    // Name validation

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

    [Fact]
    public void ValidateFailsWhenNameExceeds200Characters()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Name = new string('a', 201);

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("200", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateSucceedsWhenNameIsExactly200Characters()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Name = new string('a', 200);

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
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

    // CronExpression validation

    [Fact]
    public void ValidateFailsWhenCronExpressionIsEmpty()
    {
        // Arrange
        var request = CreateValidRequest();
        request.CronExpression = "";

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("CronExpression is required", StringComparison.Ordinal));
    }

    // Multiple failures

    [Fact]
    public void ValidateReturnsMultipleErrorsWhenAllFieldsEmpty()
    {
        // Arrange
        var request = new CreateScheduleRequest
        {
            Name = "",
            PipelineName = "",
            CronExpression = ""
        };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThanOrEqualTo(3);
    }
}
