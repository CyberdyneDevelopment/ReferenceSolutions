using Fdw.Services.Authentication.Components.ApiKeys;
using Reference.Ui.Tests.Infrastructure;
using ApiKeysPage = Fdw.UI.Pages.Authentication.Pages.ApiKeysPage;

namespace Reference.Ui.Tests.Components.Auth;

/// <summary>
/// App-level host tests for the ApiKeys page as the reference-ui app routes to it. These assert
/// ONLY that the app hosts the FDW page and that its landmark renders with a DEFAULT (unseeded)
/// provider context — they do NOT assert the FDW component's internal banner/list/form/action
/// behaviour. That internal behaviour is covered in
/// Fdw.UI.Components.Blazor.Tests (Auth/ApiKeysPageContentTests).
/// </summary>
public sealed class ApiKeysPageTests : BunitContext
{
    // Why: swap the live FDW ApiKeyProvider for a stub seeded with a DEFAULT context so the page
    // renders deterministically without HTTP — the app only owns routing/hosting here.
    private void HostDefault() =>
        ComponentFactories.Add(new ProviderFactory<ApiKeyProvider, ApiKeyContext>(new ApiKeyContext()));

    [Fact]
    public void HostsApiKeysPageRendersWithoutThrowing()
    {
        HostDefault();
        var cut = Render<ApiKeysPage>();
        cut.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HostedApiKeysPageRendersPageLandmark()
    {
        HostDefault();
        var cut = Render<ApiKeysPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.FindAll("h1").Any(h => h.TextContent.Contains("API Keys", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
