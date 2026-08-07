using System.Threading;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Scheduling.Components.Schedules;
using Moq;
using Reference.Ui.Tests.Infrastructure;
using ScheduleNewPage = Fdw.UI.Pages.Scheduling.Pages.Schedules.NewSchedulePage;

namespace Reference.Ui.Tests.Components.Pipelines;

/// <summary>
/// App-level host tests for the New Schedule page as the reference-ui app routes to it. These assert
/// ONLY that the app hosts the FDW page and that its landmark renders with a DEFAULT (unseeded)
/// provider context — they do NOT assert the FDW component's internal schedule-type field groups /
/// dropdown / validation / create behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (Schedules New page-component tests).
/// </summary>
public sealed class ScheduleNewTests : BunitContext
{
    // Why: the page itself injects IPipelineClient and calls List() unconditionally in
    // OnInitializedAsync (to populate the pipeline dropdown), so a Moq client returning an empty
    // success list must be registered, then swap the live ScheduleProvider for a stub seeded with a
    // DEFAULT context so the page renders deterministically without HTTP.
    private void HostDefault()
    {
        this.RegisterProviderInfrastructure();
        var pipelineClient = new Mock<IPipelineClient>(MockBehavior.Loose);
        pipelineClient.Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineSummaryResponse>>.Success([]));
        Services.AddSingleton(pipelineClient.Object);
        ComponentFactories.Add(new ProviderFactory<ScheduleProvider, ScheduleContext>(new ScheduleContext()));
    }

    [Fact]
    public void HostsScheduleNewPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<ScheduleNewPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedScheduleNewPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<ScheduleNewPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("New Schedule", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
