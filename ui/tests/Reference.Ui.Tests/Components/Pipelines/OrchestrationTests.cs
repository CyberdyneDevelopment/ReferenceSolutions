using Fdw.Services.Etl.Projects.UI.Components.Providers;
using Reference.Ui.Tests.Infrastructure;
using NodeListPage = Fdw.UI.Pages.EtlProjects.Pages.Orchestration.NodeListPage;
using NodeTreeEditorPage = Fdw.UI.Pages.EtlProjects.Pages.Orchestration.NodeTreeEditorPage;

namespace Reference.Ui.Tests.Components.Pipelines;

/// <summary>
/// App-level host tests for the Orchestration NodeList and NodeTreeEditor pages as the reference-ui
/// app routes to them. These assert ONLY that the app hosts each FDW page and that its landmark
/// renders with a DEFAULT (unseeded) provider context — they do NOT assert the FDW components'
/// internal error / empty / badge / search / delete or new/edit/save behaviour. That internal
/// behaviour is covered in Fdw.UI.Components.Blazor.Tests (Orchestration page-component
/// tests).
/// </summary>
public sealed class OrchestrationTests : BunitContext
{
    // Why: both pages wrap the same OrchestrationNodeProvider; swap it for a stub seeded with a
    // DEFAULT context so the pages render deterministically without HTTP.
    private void HostDefault()
    {
        this.RegisterProviderInfrastructure();
        ComponentFactories.Add(new ProviderFactory<OrchestrationNodeProvider, OrchestrationNodeContext>(new OrchestrationNodeContext()));
    }

    [Fact]
    public void HostsNodeListPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<NodeListPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedNodeListPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<NodeListPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Orchestration", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void HostsNodeTreeEditorPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<NodeTreeEditorPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedNodeTreeEditorPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<NodeTreeEditorPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("New Node", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
