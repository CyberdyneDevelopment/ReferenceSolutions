using System.Linq;
using FastEndpoints;
using Fdw.Services.Etl.JobTriggerSources;
using FluentValidation;

namespace FdwReferenceEtlJobs.Endpoints;

/// <summary>Validates <see cref="UnifiedTriggerRequest"/>.</summary>
public sealed class UnifiedTriggerRequestValidator : Validator<UnifiedTriggerRequest>
{
    /// <summary>Initializes a new instance of the <see cref="UnifiedTriggerRequestValidator"/> class.</summary>
    public UnifiedTriggerRequestValidator()
    {
        // Why required (FDW-899): what caused a run is recorded on the execution, and a missing
        // value used to be recorded as "Api" whoever the caller was. The caller states it, and it
        // must be a registered JobTriggerSourceTypes member, not free text.
        RuleFor(x => x.TriggerSource)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(source => JobTriggerSourceTypes.ByName(source!) != JobTriggerSourceTypes.NotFound)
            .WithMessage(_ => $"TriggerSource must name a registered trigger source: {string.Join(", ", JobTriggerSourceTypes.All().Select(o => o.Name))}");
    }
}
