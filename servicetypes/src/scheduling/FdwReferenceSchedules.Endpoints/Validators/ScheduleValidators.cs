using Fdw.Services.Scheduling.Abstractions.OptionTypes;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System;
using FastEndpoints;
using FluentValidation;
using Fdw.Services.Scheduling.Endpoints;

namespace FdwReferenceSchedules.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="ScheduleNameRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ScheduleNameRequestValidator : Validator<ScheduleNameRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleNameRequestValidator"/> class.
    /// </summary>
    public ScheduleNameRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Schedule name is required");
    }
}

/// <summary>
/// Validator for <see cref="CreateScheduleRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateScheduleRequestValidator : Validator<CreateScheduleRequest>
{
    /// <summary>
    /// The trigger types a schedule may declare.
    /// </summary>
    /// <remarks>
    /// Read from the collection rather than listed here, because a second list of the same names
    /// drifts from the first. That is precisely what happened before: a separate ScheduleTypes
    /// collection named four of these without carrying any behaviour, and a schedule had to be
    /// translated into a TriggerType before anything could evaluate it. Reading TriggerTypes.All()
    /// means a new trigger type is accepted the moment it is declared, with nothing to update here.
    /// </remarks>
    private static readonly string[] ValidSchedulerTypes =
        TriggerTypes.All().Select(t => t.Name).ToArray();

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateScheduleRequestValidator"/> class.
    /// </summary>
    public CreateScheduleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Schedule name is required")
            .MaximumLength(256)
            .WithMessage("Schedule name cannot exceed 256 characters")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_-]*$")
            .WithMessage("Schedule name must start with a letter and contain only letters, numbers, underscores, or hyphens");

        // The pipeline is named by id, resolved by the API proxy in its caller's view (FDW-1088); the name is the
        // one it resolved, kept for display.
        RuleFor(x => x.PipelineId)
            .NotEmpty()
            .WithMessage("Pipeline id is required");

        RuleFor(x => x.PipelineName)
            .NotEmpty()
            .WithMessage("Pipeline name is required with the pipeline id")
            .MaximumLength(200)
            .WithMessage("Pipeline name cannot exceed 200 characters");

        RuleFor(x => x.Parameters)
            .Must(ScheduleParameterMapper.NamesAreDistinct)
            .WithMessage("Every run parameter value needs a name, and no name may be given twice");

        // What the schedule's runs act with (FDW-1161): at least one role. Whether the caller holds them is the endpoint's
        // question, because it needs the caller's roles.
        RuleFor(x => x.RunRoles)
            .NotEmpty()
            .WithMessage("RunRoles is required: name at least one role the schedule's runs act with");

        RuleFor(x => x.SchedulerType)
            .NotEmpty()
            .WithMessage("Scheduler type is required (Cron, Interval, Manual, OneTime, or Event)")
            .Must(type => Array.Exists(ValidSchedulerTypes, t => string.Equals(t, type, StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Scheduler type must be one of: Cron, Interval, Manual, OneTime, Event");

        When(x => string.Equals(x.SchedulerType, "Cron", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.CronExpression)
                .NotEmpty()
                .WithMessage("Cron expression is required for Cron scheduler type")
                .MaximumLength(500)
                .WithMessage("Cron expression cannot exceed 500 characters")
                .Must(BeValidCronExpression)
                .WithMessage("Invalid cron expression format. Expected format: minute hour dayOfMonth month dayOfWeek (e.g., '0 2 * * *' for 2 AM daily)");
        });

        When(x => string.Equals(x.SchedulerType, "Interval", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.IntervalSeconds)
                .NotNull()
                .WithMessage("Interval seconds is required for Interval scheduler type")
                .GreaterThan(0)
                .WithMessage("Interval seconds must be greater than 0")
                .LessThanOrEqualTo(86400)
                .WithMessage("Interval seconds cannot exceed 86400 (24 hours)");
        });

        // Why: OneTimeScheduleType sets requiresOneTimeDateTime: true — mirror that here so creation
        // fails loudly without the date instead of persisting an unschedulable row.
        When(x => string.Equals(x.SchedulerType, "Once", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.OneTimeDateTime)
                .NotNull()
                .WithMessage("One-time date/time is required for OneTime scheduler type");
        });

        // Why: EventScheduleType sets requiresEventName: true — mirror that here.
        When(x => string.Equals(x.SchedulerType, "Event", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.EventName)
                .NotEmpty()
                .WithMessage("Event name is required for Event scheduler type")
                .MaximumLength(500)
                .WithMessage("Event name cannot exceed 500 characters");
        });
    }

    /// <summary>
    /// Validates a cron expression format (basic validation).
    /// </summary>
    private static bool BeValidCronExpression(string? cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            return false;

        // Basic cron format validation: 5 parts separated by spaces
        // minute hour dayOfMonth month dayOfWeek
        var parts = cronExpression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Standard cron has 5 parts, Quartz-style has 6 (with seconds)
        if (parts.Length is < 5 or > 6)
            return false;

        // Basic validation - each part should contain valid cron characters
        foreach (var part in parts)
        {
            if (!IsValidCronPart(part))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Validates a single cron part contains valid characters.
    /// </summary>
    private static bool IsValidCronPart(string part)
    {
        // Valid cron characters: 0-9, *, -, /, ,, L, W, #, ?
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

/// <summary>
/// Validator for <see cref="UpdateScheduleRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateScheduleRequestValidator : Validator<UpdateScheduleRequest>
{
    /// <summary>
    /// Valid scheduler types.
    /// </summary>
    /// <remarks>Read from the collection, for the same reason as above.</remarks>
    private static readonly string[] ValidSchedulerTypes =
        TriggerTypes.All().Select(t => t.Name).ToArray();

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateScheduleRequestValidator"/> class.
    /// </summary>
    public UpdateScheduleRequestValidator()
    {
        // Why: PUT is partial — clients may send just {isEnabled:false}. Only validate fields
        // when the caller actually supplied them. Required-on-update broke Newman's partial PUTs.
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Schedule name is required in the route");

        // Why: reject bodies with no updateable fields. {"name":"x"} alone is a no-op
        // (path supplies the identifier); Newman expects 400 here. The members that count, and the message that names them,
        // are UpdateScheduleChanges' one list (FDW-1170): the rule used to leave out the time zone.
        RuleFor(x => x)
            .Must(x => UpdateScheduleChanges.Stated(x))
            .WithErrorCode(UpdateScheduleChanges.NothingStatedCode)
            .WithMessage(UpdateScheduleChanges.RefusalMessage);

        RuleFor(x => x.Parameters)
            .Must(ScheduleParameterMapper.NamesAreDistinct)
            .WithMessage("Every run parameter value needs a name, and no name may be given twice");

        // Stated roles replace the recorded ones, so a list that names none is refused; leaving the field out keeps them (FDW-1161).
        When(x => x.RunRoles is not null, () =>
            RuleFor(x => x.RunRoles)
                .NotEmpty()
                .WithMessage("RunRoles, when stated, names at least one role the schedule's runs act with"));

        When(x => x.PipelineId is not null, () =>
        {
            RuleFor(x => x.PipelineId)
                .NotEmpty()
                .WithMessage("Pipeline id cannot be empty when provided");
            RuleFor(x => x.PipelineName)
                .NotEmpty()
                .WithMessage("Pipeline name is required with the pipeline id")
                .MaximumLength(200)
                .WithMessage("Pipeline name cannot exceed 200 characters");
        });

        When(x => x.PipelineId is null, () =>
        {
            RuleFor(x => x.PipelineName)
                .Null()
                .WithMessage("Pipeline name is sent only with the pipeline id");
        });

        When(x => x.SchedulerType is not null, () =>
        {
            RuleFor(x => x.SchedulerType)
                .NotEmpty()
                .WithMessage("Scheduler type cannot be empty when provided")
                .Must(type => string.IsNullOrEmpty(type) || Array.Exists(ValidSchedulerTypes, t => string.Equals(t, type, StringComparison.OrdinalIgnoreCase)))
                .WithMessage("Scheduler type must be one of: Cron, Interval, Manual, OneTime, Event");
        });

        When(x => !string.IsNullOrEmpty(x.CronExpression), () =>
        {
            RuleFor(x => x.CronExpression)
                .MaximumLength(500)
                .WithMessage("Cron expression cannot exceed 500 characters");
        });

        When(x => x.IntervalSeconds.HasValue, () =>
        {
            RuleFor(x => x.IntervalSeconds!.Value)
                .GreaterThan(0)
                .WithMessage("Interval seconds must be greater than 0")
                .LessThanOrEqualTo(86400)
                .WithMessage("Interval seconds cannot exceed 86400 (24 hours)");
        });
    }
}

/// <summary>
/// Validator for <see cref="ListSchedulesRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListSchedulesRequestValidator : Validator<ListSchedulesRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListSchedulesRequestValidator"/> class.
    /// </summary>
    public ListSchedulesRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1");

        // No upper bound here: the largest page is the MaxPaginationSize setting, which a tenant or
        // role can override, and the list caps an oversized request to it. A fixed 100 here refused
        // requests the configured maximum allows.
        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page size must be at least 1");
    }
}

/// <summary>
/// Validator for <see cref="ToggleScheduleRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ToggleScheduleRequestValidator : Validator<ToggleScheduleRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToggleScheduleRequestValidator"/> class.
    /// </summary>
    public ToggleScheduleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Schedule name is required");
    }
}
