using System;
using System.Linq;
using Reference.Scheduler.Server.Configuration;
using Reference.Scheduler.Server.Validation;
using Shouldly;
using Xunit;

namespace Reference.Scheduler.Server.Tests;

[Trait("Priority", "P1")]
[Trait("Category", "Scheduling")]
public sealed class ScheduleConfigurationValidatorTests
{
    private readonly ScheduleConfigurationValidator _sut = new();

    private static ScheduleConfiguration CreateValidCronConfig() => new()
    {
        Name = "DailySync",
        PipelineName = "MyPipeline",
        ServiceOptionType = "Cron",
        CronExpression = "0 0 * * *",
        TimeZoneId = "UTC"
    };

    private static ScheduleConfiguration CreateValidIntervalConfig() => new()
    {
        Name = "FrequentSync",
        PipelineName = "MyPipeline",
        ServiceOptionType = "Interval",
        IntervalSeconds = 300,
        TimeZoneId = "UTC"
    };

    // Name validation

    [Fact]
    public void ValidateFailsWhenNameIsEmpty()
    {
        // Arrange
        var config = CreateValidCronConfig();
        config.Name = "";

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("Name is required", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateFailsWhenNameExceeds200Characters()
    {
        // Arrange
        var config = CreateValidCronConfig();
        config.Name = new string('a', 201);

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("200", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateFailsWhenNameStartsWithNumber()
    {
        // Arrange
        var config = CreateValidCronConfig();
        config.Name = "1abc";

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("start with a letter", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateFailsWhenNameContainsSpaces()
    {
        // Arrange
        var config = CreateValidCronConfig();
        config.Name = "my schedule";

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void ValidateSucceedsWithValidName()
    {
        // Arrange
        var config = CreateValidCronConfig();

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateSucceedsWithNameContainingHyphensAndUnderscores()
    {
        // Arrange
        var config = CreateValidCronConfig();
        config.Name = "my-schedule_v2";

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    // PipelineName validation

    [Fact]
    public void ValidateFailsWhenPipelineNameIsEmpty()
    {
        // Arrange
        var config = CreateValidCronConfig();
        config.PipelineName = "";

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("PipelineName is required", StringComparison.Ordinal));
    }

    // ServiceOptionType validation

    [Fact]
    public void ValidateFailsWhenServiceOptionTypeIsEmpty()
    {
        // Arrange
        var config = CreateValidCronConfig();
        config.ServiceOptionType = "";

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("ServiceOptionType is required", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateFailsWhenServiceOptionTypeIsInvalid()
    {
        // Arrange
        var config = CreateValidCronConfig();
        config.ServiceOptionType = "Weekly";

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("must be one of", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateSucceedsForCronType()
    {
        // Arrange
        var config = CreateValidCronConfig();

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateSucceedsForIntervalType()
    {
        // Arrange
        var config = CreateValidIntervalConfig();

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateSucceedsForManualType()
    {
        // Arrange
        var config = new ScheduleConfiguration
        {
            Name = "ManualJob",
            PipelineName = "MyPipeline",
            ServiceOptionType = "Manual",
            TimeZoneId = "UTC"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateSucceedsForOneTimeType()
    {
        // Arrange
        var config = new ScheduleConfiguration
        {
            Name = "OneTimeJob",
            PipelineName = "MyPipeline",
            ServiceOptionType = "OneTime",
            TimeZoneId = "UTC",
            NextRunTime = DateTimeOffset.UtcNow.AddHours(1)
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    // Conditional Cron rules

    [Fact]
    public void ValidateFailsWhenCronTypeHasNoCronExpression()
    {
        // Arrange
        var config = CreateValidCronConfig();
        config.CronExpression = null;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("CronExpression is required", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateFailsWhenCronExpressionHasWrongPartCount()
    {
        // Arrange
        var config = CreateValidCronConfig();
        config.CronExpression = "* *";

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("Invalid cron expression", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateFailsWhenCronExpressionHasInvalidChars()
    {
        // Arrange
        var config = CreateValidCronConfig();
        config.CronExpression = "* * * * abc";

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void ValidateSucceedsWithStandard5PartCron()
    {
        // Arrange
        var config = CreateValidCronConfig();
        config.CronExpression = "0 0 * * *";

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateSucceedsWithQuartz6PartCron()
    {
        // Arrange
        var config = CreateValidCronConfig();
        config.CronExpression = "0 0 12 * * *";

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateSucceedsWithCronSpecialChars()
    {
        // Arrange
        var config = CreateValidCronConfig();
        config.CronExpression = "0/15 * 1-5 * L";

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    // Conditional Interval rules

    [Fact]
    public void ValidateFailsWhenIntervalTypeHasNoIntervalSeconds()
    {
        // Arrange
        var config = CreateValidIntervalConfig();
        config.IntervalSeconds = null;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("IntervalSeconds is required", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateFailsWhenIntervalSecondsIsZero()
    {
        // Arrange
        var config = CreateValidIntervalConfig();
        config.IntervalSeconds = 0;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("greater than 0", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateFailsWhenIntervalSecondsIsNegative()
    {
        // Arrange
        var config = CreateValidIntervalConfig();
        config.IntervalSeconds = -1;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    // TimeZoneId validation

    [Fact]
    public void ValidateFailsWhenTimeZoneIdIsEmpty()
    {
        // Arrange
        var config = CreateValidCronConfig();
        config.TimeZoneId = "";

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("TimeZoneId is required", StringComparison.Ordinal));
    }

    // IValidateOptions integration

    [Fact]
    public void ValidateOptionsReturnsSuccessForValidConfiguration()
    {
        // Arrange
        var config = CreateValidCronConfig();

        // Act
        var result = _sut.Validate(null, config);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void ValidateOptionsReturnsFailWithErrorMessages()
    {
        // Arrange
        var config = new ScheduleConfiguration { Name = "", PipelineName = "", ServiceOptionType = "" };

        // Act
        var result = _sut.Validate(null, config);

        // Assert
        result.Succeeded.ShouldBeFalse();
        result.Failures.ShouldNotBeNull();
        result.Failures!.Count().ShouldBeGreaterThan(0);
    }
}
