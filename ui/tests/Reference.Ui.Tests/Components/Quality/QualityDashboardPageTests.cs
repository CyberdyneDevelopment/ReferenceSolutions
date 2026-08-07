using Fdw.Services.Quality.Components.QualityDashboard;
using Reference.Ui.Tests.Infrastructure;
using QualityDashboardPage = Fdw.UI.Pages.Quality.Pages.Quality.QualityDashboardPage;

namespace Reference.Ui.Tests.Components.Quality;

/// <summary>
/// App-level host smoke for the Quality Dashboard page (<c>/quality</c>), an FDW page the reference-ui
/// app routes to directly. The app owns only routing/hosting, so this renders the page with a DEFAULT
/// (unseeded) <see cref="QualityDashboardContext"/> and asserts it renders without throwing and
/// surfaces the page host landmark. The page's branch coverage (loading, stats, health bar, recent
/// executions, refresh) was relocated to FDW
/// (<c>Fdw.UI.Components.Blazor.Tests/Components/Quality/QualityDashboardPageTests.cs</c>).
/// </summary>
public sealed class QualityDashboardPageTests : BunitContext
{
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<QualityDashboardProvider, QualityDashboardContext>(new QualityDashboardContext()));

    [Fact]
    public void HostsQualityDashboardRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<QualityDashboardPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedQualityDashboardRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<QualityDashboardPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Quality", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
