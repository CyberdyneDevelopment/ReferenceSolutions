using FastEndpoints;
using FluentValidation;
using ReferenceEtlJobs.Endpoints;

namespace ReferenceEtlJobs.Endpoints;

public sealed class TriggerJobRequestValidator : Validator<TriggerJobRequest>
{
    public TriggerJobRequestValidator()
    {
        RuleFor(x => x.PipelineName).NotEmpty();
    }
}
