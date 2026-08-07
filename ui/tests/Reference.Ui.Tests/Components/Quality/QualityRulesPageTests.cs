using Fdw.Services.Quality.Components.QualityRules;
using Reference.Ui.Tests.Infrastructure;
using RulesPage = Fdw.UI.Pages.Quality.Pages.Quality.QualityRulesPage;

namespace Reference.Ui.Tests.Components.Quality;

/// <summary>
/// App-level host smoke for the Quality Rules page (<c>/quality/rules</c>), an FDW page the
/// reference-ui app routes to directly. The app owns only routing/hosting, so this renders the page
/// with a DEFAULT (unseeded) <see cref="QualityRuleContext"/> and asserts it renders without throwing
/// and surfaces the page host landmark. The page's branch/action coverage (table, badges, create
/// panel, RuleType, edit, execute, delete) was relocated to FDW
/// (<c>Fdw.UI.Components.Blazor.Tests/Components/Quality/QualityRulesPageTests.cs</c>).
/// </summary>
public sealed class QualityRulesPageTests : BunitContext
{
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<QualityRuleProvider, QualityRuleContext>(new QualityRuleContext()));

    [Fact]
    public void HostsQualityRulesRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<RulesPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedQualityRulesRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<RulesPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Quality Rules", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
