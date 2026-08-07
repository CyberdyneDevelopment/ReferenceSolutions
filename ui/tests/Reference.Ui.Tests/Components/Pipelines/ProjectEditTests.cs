using Fdw.Services.Etl.Projects.UI.Components.Providers;
using Reference.Ui.Tests.Infrastructure;
using ProjectEditPage = Fdw.UI.Pages.EtlProjects.Pages.Projects.ProjectEditPage;

namespace Reference.Ui.Tests.Components.Pipelines;

/// <summary>
/// App-level host tests for the Etl.Projects ProjectEdit page as the reference-ui app routes to it.
/// These assert ONLY that the app hosts the FDW page and that its landmark renders with a DEFAULT
/// (unseeded) provider context in New mode — they do NOT assert the FDW component's internal
/// new/edit/policy/stage-designer/save behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (ProjectEdit page-component tests).
/// </summary>
public sealed class ProjectEditTests : BunitContext
{
    // Why: swap the live FDW ProjectProvider for a stub seeded with a DEFAULT context so the page
    // renders deterministically without HTTP. New mode (no Id) does not nest a StageProvider, so only
    // ProjectProvider needs swapping; the heading is "New Project".
    private void HostDefault()
    {
        this.RegisterProviderInfrastructure();
        ComponentFactories.Add(new ProviderFactory<ProjectProvider, ProjectContext>(new ProjectContext()));
    }

    [Fact]
    public void HostsProjectEditPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<ProjectEditPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedProjectEditPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<ProjectEditPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("New Project", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
