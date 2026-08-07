using Fdw.Services.SecretManagers.Components.SecretManagers;
using Fdw.UI.Pages.SecretManagers.Pages;
using Reference.Ui.Tests.Infrastructure;

namespace Reference.Ui.Tests.Components.Connections;

/// <summary>
/// App-level host tests for the Secret Managers page as the reference-ui app routes to it.
/// These assert ONLY that the app hosts the FDW page and that its landmark renders with a
/// DEFAULT (unseeded) provider context — they do NOT assert the FDW page's internal
/// card/create/edit/delete/detail behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (SecretManagers page tests).
/// </summary>
public sealed class SecretManagersPageTests : BunitContext
{
    // Why: swap the live FDW SecretManagerProvider for a stub seeded with a DEFAULT context so the
    // page renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<SecretManagerProvider, SecretManagerContext>(new SecretManagerContext()));

    [Fact]
    public void HostsSecretManagersPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<SecretManagersPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedSecretManagersPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<SecretManagersPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("Secret Managers", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
