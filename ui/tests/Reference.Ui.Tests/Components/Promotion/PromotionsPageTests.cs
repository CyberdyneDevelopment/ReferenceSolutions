using Fdw.Web.Analytics.Components.Promotions;
using Reference.Ui.Tests.Infrastructure;
using Index = Fdw.UI.Pages.Operations.Pages.Promotions.PromotionsIndexPage;

namespace Reference.Ui.Tests.Components.Promotion;

/// <summary>
/// App-level host smoke for the Promotions index page (<c>/promotions</c>), an FDW page the
/// reference-ui app routes to directly. The app owns only routing/hosting, so this renders the page
/// with a DEFAULT (unseeded) <see cref="PromotionContext"/> and asserts it renders without throwing
/// and surfaces the page host landmark. The page's branch/action coverage (table, status badges,
/// create panel, approve/reject/refresh) was relocated to FDW
/// (<c>Fdw.UI.Components.Blazor.Tests/Components/Promotion/PromotionsPageTests.cs</c>).
/// </summary>
public sealed class PromotionsPageTests : BunitContext
{
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<PromotionProvider, PromotionContext>(new PromotionContext()));

    [Fact]
    public void HostsPromotionsRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<Index>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedPromotionsRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<Index>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Promotions", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
