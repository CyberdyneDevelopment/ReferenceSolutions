using Fdw.Services.Etl.Projects.UI.Components.Providers;
using Reference.Ui.Tests.Infrastructure;
using ProjectListPage = Fdw.UI.Pages.EtlProjects.Pages.Projects.ProjectListPage;

namespace Reference.Ui.Tests.Components.Pipelines;

/// <summary>
/// App-level host tests for the Etl.Projects ProjectList page as the reference-ui app routes to it.
/// These assert ONLY that the app hosts the FDW page and that its landmark renders with a DEFAULT
/// (unseeded) provider context — they do NOT assert the FDW component's internal error / badge /
/// policy / search / run / delete behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (ProjectList page-component tests).
/// </summary>
public sealed class ProjectListTests : BunitContext
{
    // Why: swap the live FDW ProjectProvider for a stub seeded with a DEFAULT context so the page
    // renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault()
    {
        this.RegisterProviderInfrastructure();
        ComponentFactories.Add(new ProviderFactory<ProjectProvider, ProjectContext>(new ProjectContext()));
    }

    [Fact]
    public void HostsProjectListPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<ProjectListPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedProjectListPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<ProjectListPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Projects", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
