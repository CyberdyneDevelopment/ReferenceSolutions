using FluentValidation;
using Reference.Scheduler.Server.Endpoints;

namespace Reference.Scheduler.Server.Validators;

public sealed class CreateScheduleRequestValidator : AbstractValidator<CreateScheduleRequest>
{
    public CreateScheduleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.PipelineName)
            .NotEmpty()
            .WithMessage("PipelineName is required");

        RuleFor(x => x.CronExpression)
            .NotEmpty()
            .WithMessage("CronExpression is required");
    }
}
