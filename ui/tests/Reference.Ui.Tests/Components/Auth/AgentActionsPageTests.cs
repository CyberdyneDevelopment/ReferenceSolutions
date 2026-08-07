using Fdw.Agents.Components.AgentActions;
using Reference.Ui.Tests.Infrastructure;
using AgentActionsPage = Fdw.Agents.UI.Pages.Pages.AgentActionsPage;
using ReviewAgentActionPage = Fdw.Agents.UI.Pages.Pages.ReviewAgentActionPage;

namespace Reference.Ui.Tests.Components.Auth;

/// <summary>
/// App-level host tests for the Agent Actions queue and Review Agent Action pages as the
/// reference-ui app routes to them. These assert ONLY that the app hosts the FDW pages and that
/// their landmarks render with a DEFAULT (unseeded) provider context — they do NOT assert the FDW
/// component's internal queue/review branch/approve/deny behaviour. That internal behaviour is
/// covered in Fdw.UI.Components.Blazor.Tests
/// (Agents/AgentActionsPageContentTests + Agents/ReviewAgentActionPageContentTests).
/// </summary>
public sealed class AgentActionsPageTests : BunitContext
{
    // Why: swap the live FDW AgentActionProvider for a stub seeded with a DEFAULT context so the
    // page renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<AgentActionProvider, AgentActionContext>(new AgentActionContext()));

    [Fact]
    public void HostsAgentActionsPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<AgentActionsPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedAgentActionsPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<AgentActionsPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Pending Actions", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void HostsReviewAgentActionPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<ReviewAgentActionPage>(p => p.Add(r => r.ActionId, 1));
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedReviewAgentActionPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<ReviewAgentActionPage>(p => p.Add(r => r.ActionId, 1));
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Review", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
