using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceSchedulers.Quartz;

namespace FdwReferenceSchedulers.Tests;

/// <summary>
/// FDW-3788: the cron fire times a schedule missed are the times its expression names after its last run and up to now, in its own time zone; a schedule that has none
/// missed says so, and an expression or zone that cannot be read is a failure.
/// </summary>
public sealed class MissedCronFiresTests
{
    private const string DailyAt2 = "0 0 2 * * ?";

    private static DateTimeOffset At(int day, int hour, int minute = 0) => new(2026, 11, day, hour, minute, 0, TimeSpan.Zero);

    private static MissedCronFireTimes Found(string cron, string zone, DateTimeOffset last, DateTimeOffset now)
    {
        var found = MissedCronFires.Find("Nightly", cron, zone, last, now, NullLogger.Instance, TestContext.Current.CancellationToken);
        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        return found.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void ANextFireTimeThatIsStillAheadMeansNothingWasMissed()
    {
        var missed = Found(DailyAt2, "UTC", At(4, 2), At(4, 12));

        missed.Count.ShouldBe(0);
        missed.First.ShouldBeNull();
        missed.Latest.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void OneFireTimeThatPassedIsOneMissedAndIsTheLatest()
    {
        var missed = Found(DailyAt2, "UTC", At(3, 2), At(4, 3));

        missed.Count.ShouldBe(1);
        missed.First.ShouldBe(At(4, 2));
        missed.Latest.ShouldBe(At(4, 2));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void ManyFireTimesAreCountedAndTheLatestIsWhenACatchUpRunIsDue()
    {
        var missed = Found(DailyAt2, "UTC", At(1, 2), At(4, 3));

        missed.Count.ShouldBe(3);
        missed.First.ShouldBe(At(2, 2));
        missed.Latest.ShouldBe(At(4, 2));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void AFireTimeExactlyNowIsMissed()
    {
        var missed = Found(DailyAt2, "UTC", At(3, 2), At(4, 2));

        missed.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void TheExpressionIsReadInTheSchedulesOwnTimeZone()
    {
        // 08:00 in New York on 4 November 2026 is 13:00 UTC: daylight saving time ended on 1 November.
        var missed = Found("0 0 8 * * ?", "America/New_York", At(3, 13), At(4, 13, 30));

        missed.Count.ShouldBe(1);
        missed.Latest.ShouldBe(At(4, 13));
    }

    [Theory]
    [InlineData("not a cron", "UTC")]
    [InlineData(DailyAt2, "Nowhere/Land")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void AnExpressionOrZoneThatCannotBeReadIsAFailureAndNeverAnEmptyAnswer(string cron, string zone)
    {
        var found = MissedCronFires.Find("Nightly", cron, zone, At(3, 2), At(4, 3), NullLogger.Instance, TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
    }
}
