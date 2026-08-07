using System;
using System.Linq;
using FluentValidation;
using Microsoft.Extensions.Options;
using Reference.Scheduler.Server.Configuration;

namespace Reference.Scheduler.Server.Validation;

/// <summary>
/// Validator for <see cref="ScheduleConfiguration"/>.
/// </summary>
public sealed class ScheduleConfigurationValidator : AbstractValidator<ScheduleConfiguration>, IValidateOptions<ScheduleConfiguration>
{
    private static readonly string[] ValidScheduleTypes = ["Cron", "Interval", "Manual", "OneTime"];

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleConfigurationValidator"/> class.
    /// </summary>
    public ScheduleConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_-]*$")
            .WithMessage("Name must start with a letter and contain only letters, numbers, underscores, or hyphens");

        RuleFor(x => x.PipelineName)
            .NotEmpty()
            .WithMessage("PipelineName is required");

        RuleFor(x => x.ServiceOptionType)
            .NotEmpty()
            .WithMessage("ServiceOptionType is required")
            .Must(type => ValidScheduleTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"ServiceOptionType must be one of: {string.Join(", ", ValidScheduleTypes)}");

        When(x => string.Equals(x.ServiceOptionType, "Cron", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.CronExpression)
                .NotEmpty()
                .WithMessage("CronExpression is required when ServiceOptionType is 'Cron'")
                .Must(BeValidCronExpression)
                .WithMessage("Invalid cron expression format. Expected: minute hour dayOfMonth month dayOfWeek");
        });

        When(x => string.Equals(x.ServiceOptionType, "Interval", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.IntervalSeconds)
                .NotNull()
                .WithMessage("IntervalSeconds is required when ServiceOptionType is 'Interval'")
                .GreaterThan(0)
                .WithMessage("IntervalSeconds must be greater than 0");
        });

        When(x => string.Equals(x.ServiceOptionType, "OneTime", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.NextRunTime)
                .NotNull()
                .WithMessage("NextRunTime is required when ServiceOptionType is 'OneTime'")
                .Must(nextRunTime => nextRunTime > DateTimeOffset.UtcNow)
                .WithMessage("NextRunTime must be in the future when ServiceOptionType is 'OneTime'");
        });

        RuleFor(x => x.TimeZoneId)
            .NotEmpty()
            .WithMessage("TimeZoneId is required");
    }

    /// <summary>
    /// Validates the configuration options using FluentValidation rules.
    /// Called by the options framework when <c>ValidateOnStart</c> is enabled.
    /// </summary>
    /// <param name="name">The options name being validated.</param>
    /// <param name="options">The options instance to validate.</param>
    /// <returns>A <see cref="ValidateOptionsResult"/> indicating success or failure.</returns>
    public ValidateOptionsResult Validate(string? name, ScheduleConfiguration options)
    {
        var result = Validate(options);
        if (result.IsValid)
        {
            return ValidateOptionsResult.Success;
        }

        var errors = result.Errors.Select(e => e.ErrorMessage).ToArray();
        return ValidateOptionsResult.Fail(errors);
    }

    private static bool BeValidCronExpression(string? cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return false;
        }

        var parts = cronExpression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Standard cron has 5 parts, Quartz-style has 6 (with seconds)
        if (parts.Length is < 5 or > 6)
        {
            return false;
        }

        foreach (var part in parts)
        {
            if (!IsValidCronPart(part))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValidCronPart(string part)
    {
        foreach (var c in part)
        {
            if (!char.IsDigit(c) &&
                c != '*' && c != '-' && c != '/' && c != ',' &&
                c != 'L' && c != 'W' && c != '#' && c != '?')
            {
                return false;
            }
        }

        return true;
    }
}
