using Fdw.Services.Messaging.Components.Messaging;
using Reference.Ui.Tests.Infrastructure;
using AccessRequestsPage = Fdw.UI.Pages.Messaging.Pages.AccessRequestsPage;

namespace Reference.Ui.Tests.Components.Messaging;

/// <summary>
/// App-level host tests for the Access Requests page as the reference-ui app routes to it. These
/// assert ONLY that the app hosts the FDW page and that its landmark renders with a DEFAULT
/// (unseeded) provider context — they do NOT assert the FDW component's internal
/// loading/error/rows/status-badge/approve/deny-dialog/filter behaviour. That internal behaviour is
/// covered in Fdw.UI.Components.Blazor.Tests (Messaging/AccessRequestsPageContentTests).
/// </summary>
public sealed class AccessRequestsPageTests : BunitContext
{
    // Why: swap the live FDW MessageProvider for a stub seeded with a DEFAULT access-request context
    // so the page renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new MessageProviderFactory(access: new AccessRequestListContext()));

    [Fact]
    public void HostsAccessRequestsPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<AccessRequestsPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedAccessRequestsPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<AccessRequestsPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Access Requests", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
