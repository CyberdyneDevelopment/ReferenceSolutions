using Fdw.Services.Scheduling.Endpoints;
using FdwReferenceSchedules.Endpoints.Validators;

namespace FdwReferenceSchedules.Endpoints.Tests;

/// <summary>Tests that the host's create-schedule validator runs FDW's rules (FDW-3572).</summary>
public sealed class ReferenceCreateScheduleRequestValidatorTests
{
    private static CreateScheduleRequest Cron() => new()
    {
        Name = "nightly-load",
        PipelineId = Guid.NewGuid(),
        PipelineName = "Load",
        SchedulerType = "Cron",
        CronExpression = "0 2 * * *",
        MissedFireRule = "Skip",
        RunRoles = ["Admin"],
    };

    [Fact]
    public void AValidCronScheduleIsAccepted() => new ReferenceCreateScheduleRequestValidator().Validate(Cron()).IsValid.ShouldBeTrue();

    [Fact]
    public void AnFdwRuleRefusesThroughTheHostClass()
    {
        var request = Cron();
        request.RunRoles = [];

        new ReferenceCreateScheduleRequestValidator().Validate(request).Errors.ShouldContain(error => error.PropertyName == "RunRoles");
    }
}
