using Fdw.Services.Messaging.Components.Messaging;
using Reference.Ui.Tests.Infrastructure;
using DetailPage = Fdw.UI.Pages.Messaging.Pages.MessageDetailPage;

namespace Reference.Ui.Tests.Components.Messaging;

/// <summary>
/// App-level host tests for the Message Detail page as the reference-ui app routes to it. These
/// assert ONLY that the app hosts the FDW page and that its landmark renders with a DEFAULT
/// (unseeded) provider context — they do NOT assert the FDW component's internal
/// loading/not-found/detail-card/severity/sections/dismiss/archive behaviour. That internal
/// behaviour is covered in Fdw.UI.Components.Blazor.Tests
/// (Messaging/MessageDetailPageContentTests).
/// </summary>
public sealed class MessageDetailPageTests : BunitContext
{
    // Why: swap the live FDW MessageProvider for a stub seeded with a DEFAULT detail context so the
    // page renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new MessageProviderFactory(detail: new MessageDetailContext()));

    [Fact]
    public void HostsMessageDetailPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<DetailPage>(p => p.Add(d => d.Id, Guid.NewGuid()));
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedMessageDetailPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<DetailPage>(p => p.Add(d => d.Id, Guid.NewGuid()));
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Message", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
