using Fdw.Agents.Components.AgentActions;
using Fdw.Agents.UI.Pages.Pages;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components.Agent;

/// <summary>
/// App-level host tests for the Agent Actions queue page as the reference-ui app routes to it.
/// These assert ONLY that the app hosts the FDW page and that its landmark renders with a DEFAULT
/// (unseeded) provider context — they do NOT assert the FDW component's internal
/// loading/empty/rows/method-badge/status-badge/review-button/navigation behaviour. That internal
/// behaviour is covered in Fdw.UI.Components.Blazor.Tests
/// (Agents/AgentActionsPageContentTests).
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
}
