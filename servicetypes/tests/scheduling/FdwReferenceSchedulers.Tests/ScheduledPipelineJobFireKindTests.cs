using System.Globalization;
using FdwReferenceSchedulers.Quartz.Services;
using Quartz;

namespace FdwReferenceSchedulers.Tests;

/// <summary>
/// FDW-4387: an ordinary scheduled fire carries none of the catch-up and retry job-data keys, and reading an absent key with Quartz's GetString throws
/// KeyNotFoundException, which failed every ordinary fire before its pipeline started. Each key is read only where it is present; a key that is present and
/// cannot be read names itself and the fire records nothing.
/// </summary>
public sealed class ScheduledPipelineJobFireKindTests
{
    private static readonly DateTimeOffset Due = new(2026, 11, 8, 2, 0, 0, TimeSpan.Zero);

    private static string RoundTrip(DateTimeOffset time) => time.ToString("o", CultureInfo.InvariantCulture);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void AnOrdinaryFireCarriesNoneOfTheKeysAndIsScheduledAtTheTimeQuartzSays()
    {
        var kind = ScheduledPipelineJob.ReadFireKind(new JobDataMap { [ScheduledPipelineJob.ScheduleIdKey] = "s1" });

        kind.TriggerType.ShouldBe("Scheduled");
        kind.ScheduledFireTime.ShouldBeNull();
        kind.RetryAttempt.ShouldBe(0);
        kind.UnreadableKey.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void ACatchUpFireIsRecordedAsACatchUpForTheTimeItWasDue()
    {
        var kind = ScheduledPipelineJob.ReadFireKind(new JobDataMap { [ScheduledPipelineJob.CatchupDueKey] = RoundTrip(Due) });

        kind.TriggerType.ShouldBe("Catchup");
        kind.ScheduledFireTime.ShouldBe(Due.UtcDateTime);
        kind.RetryAttempt.ShouldBe(0);
        kind.UnreadableKey.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void ARetryFireIsRecordedAsARetryOfTheFireItRetriesWithItsAttempt()
    {
        var kind = ScheduledPipelineJob.ReadFireKind(new JobDataMap
        {
            [ScheduledPipelineJob.RetryAttemptKey] = "2",
            [ScheduledPipelineJob.RetriedFireKey] = RoundTrip(Due),
        });

        kind.TriggerType.ShouldBe("Retry");
        kind.ScheduledFireTime.ShouldBe(Due.UtcDateTime);
        kind.RetryAttempt.ShouldBe(2);
        kind.UnreadableKey.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ARetryOfACatchUpTakesTheRetriedFiresTime()
    {
        var retried = Due.AddHours(1);
        var kind = ScheduledPipelineJob.ReadFireKind(new JobDataMap
        {
            [ScheduledPipelineJob.CatchupDueKey] = RoundTrip(Due),
            [ScheduledPipelineJob.RetryAttemptKey] = "1",
            [ScheduledPipelineJob.RetriedFireKey] = RoundTrip(retried),
        });

        kind.TriggerType.ShouldBe("Retry");
        kind.ScheduledFireTime.ShouldBe(retried.UtcDateTime);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ARetriedFireKeyAloneDoesNotMakeAFireARetry()
    {
        // The retry is told by its attempt key; the other key is read only for a retry.
        var kind = ScheduledPipelineJob.ReadFireKind(new JobDataMap { [ScheduledPipelineJob.RetriedFireKey] = RoundTrip(Due) });

        kind.TriggerType.ShouldBe("Scheduled");
        kind.UnreadableKey.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a time")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void ACatchUpDueThatIsPresentButCannotBeReadNamesItselfAndRecordsNothing(string text)
    {
        var kind = ScheduledPipelineJob.ReadFireKind(new JobDataMap { [ScheduledPipelineJob.CatchupDueKey] = text });

        kind.UnreadableKey.ShouldBe(ScheduledPipelineJob.CatchupDueKey);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("0", true)]
    [InlineData("-1", true)]
    [InlineData("two", true)]
    [InlineData("1", false)]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void ARetryAttemptThatIsPresentButCannotBeReadNamesItselfAndRecordsNothing(string attempt, bool withRetriedFire)
    {
        var data = new JobDataMap { [ScheduledPipelineJob.RetryAttemptKey] = attempt };
        if (withRetriedFire)
        {
            data[ScheduledPipelineJob.RetriedFireKey] = RoundTrip(Due);
        }

        var kind = ScheduledPipelineJob.ReadFireKind(data);

        // "1" without its retried fire time is unreadable as well: a retry of a fire due now is never made up.
        kind.UnreadableKey.ShouldBe(ScheduledPipelineJob.RetryAttemptKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a time")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ARetriedFireThatIsPresentButCannotBeReadMakesTheRetryUnreadable(string retriedFire)
    {
        var kind = ScheduledPipelineJob.ReadFireKind(new JobDataMap
        {
            [ScheduledPipelineJob.RetryAttemptKey] = "1",
            [ScheduledPipelineJob.RetriedFireKey] = retriedFire,
        });

        kind.UnreadableKey.ShouldBe(ScheduledPipelineJob.RetryAttemptKey);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ReadingTheKindOfNoDataIsRefused()
        => Should.Throw<ArgumentNullException>(() => ScheduledPipelineJob.ReadFireKind(null!));
}
