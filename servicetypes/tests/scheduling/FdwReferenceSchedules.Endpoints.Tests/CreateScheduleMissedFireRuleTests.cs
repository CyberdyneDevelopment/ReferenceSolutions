using FdwReferenceSchedules.Endpoints.Validators;
using Fdw.Services.Scheduling.Endpoints;

namespace FdwReferenceSchedules.Endpoints.Tests;

/// <summary>
/// FDW-3788: a schedule is created with a missed-fire rule (Skip or FireOnce); a create that leaves it out or names another is refused naming the field, and no rule is
/// assumed in its place.
/// </summary>
public sealed class CreateScheduleMissedFireRuleTests
{
    private static CreateScheduleRequest Request(string rule) => new()
    {
        Name = "Nightly",
        PipelineId = Guid.NewGuid(),
        PipelineName = "monthly-sales",
        SchedulerType = "Cron",
        CronExpression = "0 0 2 * * ?",
        TimeZoneId = "UTC",
        RunRoles = ["DataWriter"],
        MissedFireRule = rule,
    };

    private static List<string> MissedFireRuleErrors(CreateScheduleRequest request) => new ReferenceCreateScheduleRequestValidator()
        .Validate(request).Errors.Where(error => string.Equals(error.PropertyName, nameof(CreateScheduleRequest.MissedFireRule), StringComparison.OrdinalIgnoreCase)).Select(error => error.ErrorMessage).ToList();

    [Theory]
    [InlineData("Skip")]
    [InlineData("FireOnce")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ARuleOfTheCollectionIsAccepted(string rule)
    {
        MissedFireRuleErrors(Request(rule)).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Catchup")]
    [InlineData("skip")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ARuleThatIsLeftOutOrNotOneOfTheCollectionsIsRefusedNamingTheField(string rule)
    {
        var errors = MissedFireRuleErrors(Request(rule));

        errors.ShouldNotBeEmpty();
        errors.ShouldAllBe(message => message.Contains("MissedFireRule", StringComparison.Ordinal));
    }
}
