using System.Text.Json;
using FdwReferenceSchedules.Endpoints.Validators;
using Fdw.Services.Scheduling.Endpoints;

namespace FdwReferenceSchedules.Endpoints.Tests;

/// <summary>
/// FDW-1170: the live update validator counts every member on <see cref="UpdateScheduleChanges"/> as a change. It once left the
/// time zone out, so a body that stated only the zone was refused as stating nothing. The scheduler type is not on that list
/// (FDW-1171): an update states it but never changes it, so a body that states only the type states nothing.
/// </summary>
public sealed class UpdateScheduleRequestValidatorTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static bool RefusedAsStatingNothing(string json) => new UpdateScheduleRequestValidator()
        .Validate(JsonSerializer.Deserialize<UpdateScheduleRequest>(json, Web)!)
        .Errors.Any(error => string.Equals(error.ErrorCode, UpdateScheduleChanges.NothingStatedCode, StringComparison.Ordinal));

    [Theory]
    [InlineData("""{ "name": "S", "pipelineId": "01a10d18-c1b3-7cf7-b8d0-4598df9f271f", "pipelineName": "Other" }""")]
    [InlineData("""{ "name": "S", "cronExpression": "0 3 * * *" }""")]
    [InlineData("""{ "name": "S", "intervalSeconds": 120 }""")]
    [InlineData("""{ "name": "S", "timeZoneId": "America/Chicago" }""")]
    [InlineData("""{ "name": "S", "isEnabled": false }""")]
    [InlineData("""{ "name": "S", "parameters": [] }""")]
    [InlineData("""{ "name": "S", "runRoles": ["Admin"] }""")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void EveryMemberOnTheListAloneIsAChange(string json)
    {
        RefusedAsStatingNothing(json).ShouldBeFalse();
    }

    [Theory]
    [InlineData("""{ "name": "S" }""")]
    [InlineData("""{ "name": "S", "timeZoneId": null }""")]
    [InlineData("""{ "name": "S", "schedulerType": "Cron" }""")]
    [InlineData("""{ "name": "S", "schedulerType": "Interval" }""")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ABodyThatStatesNothingIsRefusedWithTheMessageThatNamesEveryMember(string json)
    {
        RefusedAsStatingNothing(json).ShouldBeTrue();
    }
}
