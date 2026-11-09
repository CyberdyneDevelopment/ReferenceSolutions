using System.Globalization;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Execution.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using FdwReferenceSchedulers.Quartz.Services;
using Quartz;

namespace FdwReferenceSchedulers.Tests;

/// <summary>
/// FDW-4387: <see cref="ScheduledPipelineJob.Execute"/> itself, not only the reading of the fire's keys. An ordinary fire carries none of the catch-up and retry keys and must
/// reach the handler; a catch-up and a retry reach it for the time their data names; a key that is present and unreadable stops the fire before the handler is called.
/// </summary>
public sealed class ScheduledPipelineJobExecuteTests
{
    private const string ProcessId = "Nightly";
    private const string ScheduleId = "NightlySchedule";

    private static readonly DateTimeOffset ScheduledFor = new(2026, 11, 8, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fired = ScheduledFor.AddSeconds(1);
    private static readonly DateTimeOffset Named = new(2026, 11, 7, 2, 0, 0, TimeSpan.Zero);

    private static string RoundTrip(DateTimeOffset time) => time.ToString("o", CultureInfo.InvariantCulture);

    private sealed class Harness
    {
        public Mock<IGenericScheduledExecutionHandler> Handler { get; } = new();

        public ScheduledPipelineJob Job { get; }

        public Harness()
        {
            var result = new Mock<IGenericResult<IProcessResult>>();
            result.SetupGet(r => r.IsSuccess).Returns(true);
            Handler
                .Setup(h => h.ExecuteScheduledProcess(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                    It.IsAny<IReadOnlyDictionary<string, object>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result.Object);

            // The history and run-field writes answer a result the job only reads; this test is about which fire reaches the handler.
            var gateways = new Mock<IDataGatewayProvider>();
            gateways.Setup(g => g.ByName(It.IsAny<string>())).Returns(new Mock<IDataGateway> { DefaultValue = DefaultValue.Mock }.Object);

            Job = new ScheduledPipelineJob(
                Handler.Object,
                gateways.Object,
                new Mock<IAuthenticationContextAccessor>().Object,
                new Mock<IScheduleConfigurationProvider>().Object);
        }

        public Task Fire(JobDataMap triggerData)
        {
            var jobData = new JobDataMap
            {
                [ScheduledPipelineJob.ProcessIdKey] = ProcessId,
                [ScheduledPipelineJob.ScheduleIdKey] = ScheduleId,
                [ScheduledPipelineJob.DataStoreNameKey] = "Main",
                [ScheduledPipelineJob.PathNameKey] = "sched",
                [ScheduledPipelineJob.ScheduleContainerNameKey] = "Schedule",
            };
            // Why built by copying: JobDataMap implements IDictionary<string, object> and IDictionary, so passing it to the JobDataMap constructor is ambiguous (CS0121).
            var merged = new JobDataMap();
            foreach (var pair in jobData)
            {
                merged[pair.Key] = pair.Value;
            }

            foreach (var pair in triggerData)
            {
                merged[pair.Key] = pair.Value;
            }

            var scheduler = new Mock<IScheduler>();
            scheduler
                .Setup(s => s.GetTriggersOfJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<ITrigger>());

            var context = new Mock<IJobExecutionContext>();
            context.SetupGet(c => c.JobDetail).Returns(JobBuilder.Create<ScheduledPipelineJob>().WithIdentity("job", "group").UsingJobData(jobData).Build());
            context.SetupGet(c => c.MergedJobDataMap).Returns(merged);
            context.SetupGet(c => c.ScheduledFireTimeUtc).Returns(ScheduledFor);
            context.SetupGet(c => c.FireTimeUtc).Returns(Fired);
            context.SetupGet(c => c.NextFireTimeUtc).Returns(ScheduledFor.AddDays(1));
            context.SetupGet(c => c.Scheduler).Returns(scheduler.Object);
            context.SetupGet(c => c.CancellationToken).Returns(TestContext.Current.CancellationToken);

            return Job.Execute(context.Object);
        }

        public void VerifyStartedFor(DateTimeOffset scheduledFor)
            => Handler.Verify(h => h.ExecuteScheduledProcess(
                ProcessId, ScheduleId, scheduledFor.UtcDateTime, Fired.UtcDateTime,
                It.IsAny<IReadOnlyDictionary<string, object>?>(), It.IsAny<CancellationToken>()), Times.Once);

        public void VerifyNothingStarted()
            => Handler.Verify(h => h.ExecuteScheduledProcess(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<IReadOnlyDictionary<string, object>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task AnOrdinaryFireStartsItsPipelineForTheTimeQuartzScheduledIt()
    {
        var harness = new Harness();

        await harness.Fire(new JobDataMap());

        harness.VerifyStartedFor(ScheduledFor);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task ACatchUpFireStartsItsPipelineForTheTimeItWasDue()
    {
        var harness = new Harness();

        await harness.Fire(new JobDataMap { [ScheduledPipelineJob.CatchupDueKey] = RoundTrip(Named) });

        harness.VerifyStartedFor(Named);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task ARetryFireStartsItsPipelineForTheTimeOfTheFireItRetries()
    {
        var harness = new Harness();

        await harness.Fire(new JobDataMap
        {
            [ScheduledPipelineJob.RetryAttemptKey] = "1",
            [ScheduledPipelineJob.RetriedFireKey] = RoundTrip(Named),
        });

        harness.VerifyStartedFor(Named);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task ACatchUpDueThatCannotBeReadStartsNothing()
    {
        var harness = new Harness();

        await harness.Fire(new JobDataMap { [ScheduledPipelineJob.CatchupDueKey] = "not a time" });

        harness.VerifyNothingStarted();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task ARetryWithoutItsRetriedFireTimeStartsNothing()
    {
        var harness = new Harness();

        await harness.Fire(new JobDataMap { [ScheduledPipelineJob.RetryAttemptKey] = "1" });

        harness.VerifyNothingStarted();
    }
}
