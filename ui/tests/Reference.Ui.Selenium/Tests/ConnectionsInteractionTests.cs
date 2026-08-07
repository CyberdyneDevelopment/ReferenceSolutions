using OpenQA.Selenium;
using Reference.Ui.Selenium.Infrastructure;
using Reference.Ui.Selenium.Pages;

namespace Reference.Ui.Selenium.Tests;

/// <summary>
/// Read-only interaction exemplars on /connections (card-grid list page). These demonstrate the
/// list-page interaction patterns (wait-for-items, @bind-onchange search, row navigation) that
/// per-area suites copy. STRICTLY read-only — no mutation is ever submitted against the shared slot.
/// </summary>
[Collection("ui-selenium")]
[Trait("Category", "Ui")]
public sealed class ConnectionsInteractionTests(SeleniumFixture fixture)
{
    // Why: the connections list renders as an HTML table in this FDW line (the card grid the
    // Playwright suite targeted was the rc.1.3 markup) — one row per connection.
    private const string ItemSelector = "tbody tr";

    private static ListPage OpenList(IWebDriver driver) =>
        new(driver, SeleniumFixture.BaseUrl, "/connections", ItemSelector);

    [Fact]
    public void ListRendersSeededConnections()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");

        var driver = fixture.NewAuthenticatedDriver();
        var list = OpenList(driver);
        list.Goto();
        // Seeded baseline always includes the ConfigurationDb self-connection.
        list.WaitForItems().ShouldBeGreaterThan(0);
        list.ItemContaining("ConfigurationDb").ShouldNotBeNull();
    }

    [Fact]
    public void SearchNarrowsVisibleConnections()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");

        var driver = fixture.NewAuthenticatedDriver();
        var list = OpenList(driver);
        list.Goto();
        var all = list.WaitForItems();

        list.Search("ConfigurationDb");
        var narrowed = list.Items.Count(e => e.Displayed);
        narrowed.ShouldBeGreaterThan(0);
        narrowed.ShouldBeLessThanOrEqualTo(all);
        list.ItemContaining("ConfigurationDb").ShouldNotBeNull();
    }

    [Fact]
    public void ClickingConnectionOpensDetailWithoutError()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");

        var driver = fixture.NewAuthenticatedDriver();
        var list = OpenList(driver);
        list.Goto();
        list.WaitForItems();

        var row = list.ItemContaining("ConfigurationDb");
        row.ShouldNotBeNull();
        // Why: the row carries per-row action buttons (Test/Delete) — clicking those would mutate.
        // The name cell is the navigation target.
        row.FindElement(By.CssSelector(".nm")).Click();

        PageAssertions.WaitForBlazor(driver);
        PageAssertions.ShouldBeAuthenticated(driver);
        PageAssertions.ShouldNotShowError(driver);
        driver.Url.ShouldNotEndWith("/connections"); // navigated off the list
    }

    [Fact]
    public void NewConnectionPageRendersWizard()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");

        var driver = fixture.NewAuthenticatedDriver();
        driver.Navigate().GoToUrl($"{SeleniumFixture.BaseUrl}/connections/new");
        PageAssertions.ShouldBeAuthenticated(driver);
        PageAssertions.ShouldNotShowError(driver);
        // The wizard's first step must render at least one form control. READ-ONLY: never submit.
        driver.FindElements(By.CssSelector("input, select, textarea"))
            .Count(e => e.Displayed).ShouldBeGreaterThan(0);
    }
}
