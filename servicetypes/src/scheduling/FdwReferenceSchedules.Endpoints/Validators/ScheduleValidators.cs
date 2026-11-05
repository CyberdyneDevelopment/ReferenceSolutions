using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using FluentValidation;
using Fdw.Services.Scheduling.Endpoints;
using Fdw.Services.Scheduling.Endpoints.Validators;

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
/// The create-schedule rules of this host: FDW's (<see cref="CreateScheduleRequestValidator"/>), with nothing the host adds (FDW-3572).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ReferenceCreateScheduleRequestValidator : CreateScheduleRequestValidator;

/// <summary>
/// The update-schedule rules of this host: FDW's (<see cref="UpdateScheduleRequestValidator"/>), with nothing the host adds (FDW-3572).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ReferenceUpdateScheduleRequestValidator : UpdateScheduleRequestValidator;

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
