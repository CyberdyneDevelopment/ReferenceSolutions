using Fdw.Web.Analytics.Components.PromotionReview;
using Reference.Ui.Tests.Infrastructure;
using ReviewPage = Fdw.UI.Pages.Operations.Pages.Promotions.PromotionReviewPage;

namespace Reference.Ui.Tests.Components.Promotion;

/// <summary>
/// App-level host smoke for the Promotion Review page (<c>/promotions/{Id}/review</c>), an FDW page
/// the reference-ui app routes to directly. The app owns only routing/hosting, so this renders the
/// page with a DEFAULT (unseeded) <see cref="PromotionReviewContext"/> and the route <c>Id</c>
/// supplied, asserting it renders without throwing and surfaces the page host landmark. The page's
/// branch/action coverage (detail card, status badges, approve/reject) was relocated to FDW
/// (<c>Fdw.UI.Components.Blazor.Tests/Components/Promotion/PromotionReviewPageTests.cs</c>).
/// </summary>
public sealed class PromotionReviewPageTests : BunitContext
{
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<PromotionReviewProvider, PromotionReviewContext>(new PromotionReviewContext()));

    [Fact]
    public void HostsPromotionReviewRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<ReviewPage>(p => p.Add(r => r.Id, Guid.NewGuid()));
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedPromotionReviewRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<ReviewPage>(p => p.Add(r => r.Id, Guid.NewGuid()));
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Review Promotion", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
