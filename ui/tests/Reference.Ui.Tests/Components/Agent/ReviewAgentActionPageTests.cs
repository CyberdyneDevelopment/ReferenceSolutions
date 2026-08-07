using Fdw.Agents.Components.AgentActions;
using Fdw.Agents.UI.Pages.Pages;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components.Agent;

/// <summary>
/// App-level host tests for the Review Agent Action page as the reference-ui app routes to it.
/// These assert ONLY that the app hosts the FDW page and that its landmark renders with a DEFAULT
/// (unseeded) provider context — they do NOT assert the FDW component's internal
/// loading/not-found/already-reviewed/review-mode/approve/deny/navigation behaviour. That internal
/// behaviour is covered in Fdw.UI.Components.Blazor.Tests
/// (Agents/ReviewAgentActionPageContentTests).
/// </summary>
public sealed class ReviewAgentActionPageTests : BunitContext
{
    // Why: swap the live FDW AgentActionProvider for a stub seeded with a DEFAULT context so the
    // page renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<AgentActionProvider, AgentActionContext>(new AgentActionContext()));

    [Fact]
    public void HostsReviewAgentActionPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<ReviewAgentActionPage>(p => p.Add(r => r.ActionId, 7));
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedReviewAgentActionPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<ReviewAgentActionPage>(p => p.Add(r => r.ActionId, 7));
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Review", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
