using FastEndpoints;
using FluentValidation;
using Reference.Etl.Server.Endpoints;

namespace Reference.Etl.Server.Validators;

public sealed class TriggerJobRequestValidator : Validator<TriggerJobRequest>
{
    public TriggerJobRequestValidator()
    {
        RuleFor(x => x.PipelineName).NotEmpty();
    }
}
