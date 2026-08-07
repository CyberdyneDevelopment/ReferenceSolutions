using FluentValidation;
using Reference.Scheduler.Server.Endpoints;

namespace Reference.Scheduler.Server.Validators;

public sealed class UpdateScheduleRequestValidator : AbstractValidator<UpdateScheduleRequest>
{
    public UpdateScheduleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.PipelineName)
            .NotEmpty()
            .WithMessage("PipelineName is required");
    }
}
