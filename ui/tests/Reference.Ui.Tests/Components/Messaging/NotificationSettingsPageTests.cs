using System.Net.Http;
using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Reference.Ui.Tests.Infrastructure;
using NotificationSettingsPage = Fdw.UI.Pages.Messaging.Pages.NotificationSettingsPage;

namespace Reference.Ui.Tests.Components.Messaging;

/// <summary>
/// App-level host tests for the Notification Settings page as the reference-ui app routes to it.
/// Unlike the provider-stubbed pages this one drives a real NotificationApiClient over HttpClient,
/// so a no-op HTTP factory + an authenticated user are wired so the page boots. These assert ONLY
/// that the app hosts the FDW page and that its landmark renders — they do NOT assert the FDW
/// component's internal preference-row/toggle/save/reset behaviour. That internal behaviour is
/// covered in Fdw.UI.Components.Blazor.Tests (Messaging/NotificationSettingsPageContentTests).
/// </summary>
public sealed class NotificationSettingsPageTests : BunitContext
{
    // Why: the page resolves a real NotificationApiClient and reads the authenticated user's id, so
    // host-smoke needs a no-op HTTP factory + a logger factory + an authorized user — but no seeded
    // preference data; the page header renders regardless of the (empty) load result.
    private void HostDefault()
    {
        var handler = new MockHttpHandler().WithDefault("[]");
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
               .Returns(() => new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") });
        Services.AddSingleton(factory.Object);
        Services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        var authContext = this.AddAuthorization();
        authContext.SetAuthorized("tester");
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
    }

    [Fact]
    public void HostsNotificationSettingsPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<NotificationSettingsPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedNotificationSettingsPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<NotificationSettingsPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Notification Preferences", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
