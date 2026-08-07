using Fdw.Operations.Components.Execution;
using Reference.Ui.Tests.Infrastructure;
using ExecutionDetailPage = Fdw.UI.Pages.Pipelines.Pages.Pipelines.ExecutionDetailPage;

namespace Reference.Ui.Tests.Components.Pipelines;

/// <summary>
/// App-level host tests for the pipeline ExecutionDetail page as the reference-ui app routes to it.
/// These assert ONLY that the app hosts the FDW page and that its landmark renders with a DEFAULT
/// (unseeded) provider context — they do NOT assert the FDW component's internal loading / error /
/// summary-card / steps-table / state-badge / refresh behaviour. That internal behaviour is covered
/// in Fdw.UI.Components.Blazor.Tests (ExecutionDetail page-component tests).
/// </summary>
public sealed class ExecutionDetailTests : BunitContext
{
    // Why: swap the live FDW ExecutionDetailProvider for a stub seeded with a DEFAULT context so the
    // page renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault()
    {
        this.RegisterProviderInfrastructure();
        ComponentFactories.Add(new ProviderFactory<ExecutionDetailProvider, ExecutionDetailContext>(new ExecutionDetailContext()));
    }

    [Fact]
    public void HostsExecutionDetailPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<ExecutionDetailPage>(p => p.Add(x => x.ExecutionId, Guid.NewGuid()));
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedExecutionDetailPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<ExecutionDetailPage>(p => p.Add(x => x.ExecutionId, Guid.NewGuid()));
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Execution Detail", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
