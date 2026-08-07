using Fdw.Services.Messaging.Components.Messaging;
using Reference.Ui.Tests.Infrastructure;
using MessagesPage = Fdw.UI.Pages.Messaging.Pages.MessagesPage;

namespace Reference.Ui.Tests.Components.Messaging;

/// <summary>
/// App-level host tests for the Messages list page as the reference-ui app routes to it. These
/// assert ONLY that the app hosts the FDW page and that its landmark renders with a DEFAULT
/// (unseeded) provider context — they do NOT assert the FDW component's internal
/// rows/filters/severity/dismiss/archive/navigation behaviour. That internal behaviour is covered
/// in Fdw.UI.Components.Blazor.Tests (Messaging/MessagesPageContentTests).
/// </summary>
public sealed class MessagesPageTests : BunitContext
{
    // Why: swap the live FDW MessageProvider for a stub seeded with a DEFAULT list context so the
    // page renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new MessageProviderFactory(list: new MessageListContext()));

    [Fact]
    public void HostsMessagesPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<MessagesPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedMessagesPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<MessagesPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Messages", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
