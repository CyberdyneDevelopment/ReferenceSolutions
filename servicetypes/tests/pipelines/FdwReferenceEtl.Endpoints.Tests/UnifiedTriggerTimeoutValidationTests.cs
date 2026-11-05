using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.JobTriggerSources;
using FdwReferenceEtlJobs.Endpoints;

namespace FdwReferenceEtl.Endpoints.Tests;

/// <summary>
/// Tests for the timeout a pipeline trigger may state (FDW-3805): none is a run with no time limit, a stated one is positive and no longer than the timer can wait, and
/// nothing is cut short or filled in.
/// </summary>
public sealed class UnifiedTriggerTimeoutValidationTests
{
    private static readonly UnifiedTriggerRequestValidator Validator = new();

    private static UnifiedTriggerRequest Request(int? timeoutSeconds) => new()
    {
        Name = "monthly-sales",
        TriggerSource = JobTriggerSourceTypes.Scheduled.Name,
        ScheduleName = "Nightly",
        TimeoutSeconds = timeoutSeconds,
    };

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(900)]
    [InlineData(PipelineExecutionRequest.MaxTimeoutSeconds)]
    public void ATimeoutThatIsLeftOutOrInRangeIsAccepted(int? timeoutSeconds)
    {
        Validator.Validate(Request(timeoutSeconds)).IsValid.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(PipelineExecutionRequest.MaxTimeoutSeconds + 1)]
    public void ATimeoutThatStopsTheRunBeforeItBeginsOrCannotBeKeptIsRefusedNamingTheField(int timeoutSeconds)
    {
        var result = Validator.Validate(Request(timeoutSeconds));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain(nameof(UnifiedTriggerRequest.TimeoutSeconds));
    }
}
