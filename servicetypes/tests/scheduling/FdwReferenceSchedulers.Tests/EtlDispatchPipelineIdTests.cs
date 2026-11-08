using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using FdwReferenceSchedulers.EtlDispatch;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace FdwReferenceSchedulers.Tests;

/// <summary>
/// A scheduled job carries the pipeline's id and the dispatcher relays that id to the ETL server (FDW-1088), so a
/// run resolves exactly the pipeline the schedule stores, never another tenant's of the same name.
/// </summary>
public sealed class EtlDispatchPipelineIdTests
{
    private static readonly Guid PipelineId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TenantId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ScheduleId = new("44444444-4444-4444-4444-444444444444");

    private readonly Mock<IPipelineJobClient> _client = new();
    private readonly Mock<IScheduleConfigurationProvider> _schedules = new();

    private EtlDispatchExecutionHandler Build() => new(_client.Object, _schedules.Object, NullLogger<EtlDispatchExecutionHandler>.Instance);

    private void Schedule(Guid pipelineId, params string[] runRoles) => Schedule(pipelineId, null, runRoles);

    private void Schedule(Guid pipelineId, int? timeoutSeconds, params string[] runRoles)
    {
        var schedule = new ScheduleImplementationConfiguration { Id = ScheduleId, Name = "NightlyAt2", PipelineId = pipelineId, PipelineName = "Nightly", TenantId = TenantId };
        if (timeoutSeconds is { } seconds)
        {
            schedule.TimeoutSeconds = seconds;
        }

        schedule.ReplaceRunRoles(runRoles);
        _schedules
            .Setup(s => s.Get("NightlyAt2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IScheduleImplementationConfiguration>.Success(schedule));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheRunIsTriggeredByThePipelineIdWithNoName()
    {
        Schedule(PipelineId);
        TriggerPipelineRequest? sent = null;
        _client
            .Setup(c => c.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .Callback((TriggerPipelineRequest r, CancellationToken _) => sent = r)
            .ReturnsAsync(GenericResult<TriggerPipelineResponse>.Success(new TriggerPipelineResponse { ExecutionId = Guid.NewGuid() }));

        var result = await Build().ExecuteScheduledProcess(PipelineId.ToString("D"), "NightlyAt2", DateTime.UtcNow, DateTime.UtcNow,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        sent.ShouldNotBeNull();
        sent.Id.ShouldBe(PipelineId);
        sent.Name.ShouldBeEmpty();
        sent.TenantId.ShouldBe(TenantId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheRunActsWithTheRolesTheScheduleRecorded()
    {
        Schedule(PipelineId, "DataEngineer", "Analyst");
        TriggerPipelineRequest? sent = null;
        _client
            .Setup(c => c.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .Callback((TriggerPipelineRequest r, CancellationToken _) => sent = r)
            .ReturnsAsync(GenericResult<TriggerPipelineResponse>.Success(new TriggerPipelineResponse { ExecutionId = Guid.NewGuid() }));

        var result = await Build().ExecuteScheduledProcess(PipelineId.ToString("D"), "NightlyAt2", DateTime.UtcNow, DateTime.UtcNow,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        sent.ShouldNotBeNull().RunRoles.ShouldNotBeNull().ShouldBe(["DataEngineer", "Analyst"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AJobStillHoldingAPipelineTheScheduleNoLongerNamesIsNotRun()
    {
        Schedule(Guid.NewGuid());

        var result = await Build().ExecuteScheduledProcess(PipelineId.ToString("D"), "NightlyAt2", DateTime.UtcNow, DateTime.UtcNow,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _client.Verify(c => c.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AProcessIdThatIsNotAPipelineIdIsNotRun()
    {
        Schedule(PipelineId);

        var result = await Build().ExecuteScheduledProcess("Nightly", "NightlyAt2", DateTime.UtcNow, DateTime.UtcNow,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _client.Verify(c => c.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheScheduleParametersAreRelayedToTheTriggerOnEveryRun()
    {
        _schedules
            .Setup(s => s.Get("NightlyAt2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IScheduleImplementationConfiguration>.Success(new ScheduleImplementationConfiguration
            {
                Name = "NightlyAt2",
                PipelineId = PipelineId,
                PipelineName = "Nightly",
                TenantId = TenantId,
                Parameters = new Dictionary<string, string?> { ["Since"] = "2026-10-01T00:00:00Z", ["Region"] = "EU" },
            }));
        TriggerPipelineRequest? sent = null;
        _client
            .Setup(c => c.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .Callback((TriggerPipelineRequest r, CancellationToken _) => sent = r)
            .ReturnsAsync(GenericResult<TriggerPipelineResponse>.Success(new TriggerPipelineResponse { ExecutionId = Guid.NewGuid() }));

        var result = await Build().ExecuteScheduledProcess(PipelineId.ToString("D"), "NightlyAt2", DateTime.UtcNow, DateTime.UtcNow,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        sent.ShouldNotBeNull();
        sent.Parameters.ShouldNotBeNull();
        sent.Parameters.Select(p => (p.Name, p.Value)).OrderBy(p => p.Name).ShouldBe([("Region", "EU"), ("Since", "2026-10-01T00:00:00Z")]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheScheduleNameItsIdAndTheTimeoutItStatesAreRelayedToTheEtlServer()
    {
        Schedule(PipelineId, 900, "DataWriter");
        TriggerPipelineRequest? sent = null;
        _client
            .Setup(c => c.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .Callback((TriggerPipelineRequest r, CancellationToken _) => sent = r)
            .ReturnsAsync(GenericResult<TriggerPipelineResponse>.Success(new TriggerPipelineResponse { ExecutionId = Guid.NewGuid() }));

        var result = await Build().ExecuteScheduledProcess(PipelineId.ToString("D"), "NightlyAt2", DateTime.UtcNow, DateTime.UtcNow,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        sent.ShouldNotBeNull();
        sent.ScheduleName.ShouldBe("NightlyAt2");
        sent.ScheduleId.ShouldBe(ScheduleId);
        sent.TimeoutSeconds.ShouldBe(900);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task EveryAttemptOfOneFireSendsTheSameKeyAndANewFireSendsAnother()
    {
        // Why: the ETL host answers a retry of a fire (after a 504) with the run the first call started only when the key is the same (FDW-4317).
        Schedule(PipelineId);
        var sent = new List<TriggerPipelineRequest>();
        _client
            .Setup(c => c.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .Callback((TriggerPipelineRequest r, CancellationToken _) => sent.Add(r))
            .ReturnsAsync(GenericResult<TriggerPipelineResponse>.Success(new TriggerPipelineResponse { ExecutionId = Guid.NewGuid() }));
        var due = new DateTime(2026, 11, 7, 2, 0, 0, DateTimeKind.Utc);

        _ = await Build().ExecuteScheduledProcess(PipelineId.ToString("D"), "NightlyAt2", due, due.AddSeconds(1), cancellationToken: TestContext.Current.CancellationToken);
        _ = await Build().ExecuteScheduledProcess(PipelineId.ToString("D"), "NightlyAt2", due, due.AddSeconds(31), cancellationToken: TestContext.Current.CancellationToken);
        _ = await Build().ExecuteScheduledProcess(PipelineId.ToString("D"), "NightlyAt2", due.AddDays(1), due.AddDays(1), cancellationToken: TestContext.Current.CancellationToken);

        sent.Count.ShouldBe(3);
        sent[0].TriggerKey.ShouldNotBeNullOrWhiteSpace();
        sent[0].TriggerKey.ShouldStartWith(ScheduleId.ToString("D"));
        sent[1].TriggerKey.ShouldBe(sent[0].TriggerKey);
        sent[2].TriggerKey.ShouldNotBe(sent[0].TriggerKey);
        sent[0].TriggerKeyFireTime.ShouldBe(new DateTimeOffset(due));
    }
}
