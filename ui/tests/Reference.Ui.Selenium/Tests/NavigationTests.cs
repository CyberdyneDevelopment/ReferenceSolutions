using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reference.Ui.Selenium.Infrastructure;
using Reference.Ui.Selenium.Pages;

namespace Reference.Ui.Selenium.Tests;

/// <summary>
/// App-shell exemplars: the hardcoded sidebar (Components/Layout/MainLayout.razor —
/// <c>aside.sidebar &gt; nav.nav</c>) is the one piece of information architecture the skin owns,
/// so it gets its own tests. Blazor navigates in-circuit (no page load event) — assert on URL
/// change via explicit wait, mirroring the Playwright suite's ToHaveURL pattern.
/// </summary>
[Collection("ui-selenium")]
[Trait("Category", "Ui")]
public sealed class NavigationTests(SeleniumFixture fixture)
{
    [Fact]
    public void SidebarShowsMajorAreas()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");

        var driver = fixture.NewAuthenticatedDriver();
        var links = driver.FindElements(By.CssSelector("aside.sidebar nav.nav a"))
            .Select(a => a.GetDomAttribute("href"))
            .Where(h => h is not null)
            .ToList();

        foreach (var expected in new[] { "/connections", "/datastores", "/pipelines", "/settings" })
            links.ShouldContain(h => h!.EndsWith(expected, StringComparison.Ordinal),
                $"sidebar is missing a link to {expected}");
    }

    [Fact]
    public void SidebarNavigatesToConnections()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");

        var driver = fixture.NewAuthenticatedDriver();
        driver.FindElement(By.CssSelector("aside.sidebar nav.nav a[href$='/connections']")).Click();

        new WebDriverWait(driver, TimeSpan.FromSeconds(15))
            .Until(d => d.Url.EndsWith("/connections", StringComparison.Ordinal));
        PageAssertions.ShouldNotShowError(driver);
    }

    [Fact]
    public void LogoutReturnsToLoginAndKillsSession()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");

        // Why: logging out of the SHARED fixture session would break every later test, so this
        // test builds its own throwaway session through the real login form.
        var driver = fixture.NewDriver();
        var loginPage = new LoginPage(driver, SeleniumFixture.BaseUrl);
        loginPage.Goto();
        loginPage.Login(E2ESettings.Username, E2ESettings.Password);
        new WebDriverWait(driver, TimeSpan.FromSeconds(30))
            .Until(d => !d.Url.Contains("/login", StringComparison.Ordinal));

        driver.Navigate().GoToUrl($"{SeleniumFixture.BaseUrl}/auth/logout");
        new WebDriverWait(driver, TimeSpan.FromSeconds(15))
            .Until(d => d.Url.Contains("/login", StringComparison.Ordinal));

        // The session is dead: a protected route must bounce straight back to /login.
        driver.Navigate().GoToUrl($"{SeleniumFixture.BaseUrl}/connections");
        new WebDriverWait(driver, TimeSpan.FromSeconds(15))
            .Until(d => d.Url.Contains("/login", StringComparison.Ordinal));
    }
}
