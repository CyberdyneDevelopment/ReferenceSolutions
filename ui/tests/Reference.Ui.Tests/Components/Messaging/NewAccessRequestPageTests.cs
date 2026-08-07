using Fdw.Services.Messaging.Components.Messaging;
using Reference.Ui.Tests.Infrastructure;
using NewAccessRequestPage = Fdw.UI.Pages.Messaging.Pages.NewAccessRequestPage;

namespace Reference.Ui.Tests.Components.Messaging;

/// <summary>
/// App-level host tests for the New Access Request page as the reference-ui app routes to it. These
/// assert ONLY that the app hosts the FDW page and that its landmark renders with a DEFAULT
/// (unseeded) provider context — they do NOT assert the FDW component's internal
/// form/validation/submit/cancel behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (Messaging/NewAccessRequestPageContentTests).
/// </summary>
public sealed class NewAccessRequestPageTests : BunitContext
{
    // Why: swap the live FDW MessageProvider for a stub seeded with a DEFAULT access-request context
    // so the page renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new MessageProviderFactory(access: new AccessRequestListContext()));

    [Fact]
    public void HostsNewAccessRequestPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<NewAccessRequestPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedNewAccessRequestPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<NewAccessRequestPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Request Access", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
