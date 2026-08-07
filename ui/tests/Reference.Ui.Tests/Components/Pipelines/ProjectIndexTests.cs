using Fdw.Services.Etl.Projects.UI.Components.Providers;
using Reference.Ui.Tests.Infrastructure;
using ProjectIndexPage = Fdw.UI.Pages.EtlProjects.Pages.Projects.ProjectIndexPage;

namespace Reference.Ui.Tests.Components.Pipelines;

/// <summary>
/// App-level host tests for the Etl.Projects ProjectIndex (project tree) page as the reference-ui
/// app routes to it. These assert ONLY that the app hosts the FDW page and that its landmark renders
/// with a DEFAULT (unseeded) provider context — they do NOT assert the FDW component's internal
/// error / empty / populated / expand-collapse / run behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (ProjectIndex page-component tests).
/// </summary>
public sealed class ProjectIndexTests : BunitContext
{
    // Why: swap the live FDW ProjectProvider for a stub seeded with a DEFAULT context so the page
    // renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault()
    {
        this.RegisterProviderInfrastructure();
        ComponentFactories.Add(new ProviderFactory<ProjectProvider, ProjectContext>(new ProjectContext()));
    }

    [Fact]
    public void HostsProjectIndexPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<ProjectIndexPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedProjectIndexPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<ProjectIndexPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Project Tree", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
