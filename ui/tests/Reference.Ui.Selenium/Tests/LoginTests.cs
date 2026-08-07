using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reference.Ui.Selenium.Infrastructure;
using Reference.Ui.Selenium.Pages;

namespace Reference.Ui.Selenium.Tests;

[Collection("ui-selenium")]
[Trait("Category", "Ui")]
public sealed class LoginTests(SeleniumFixture fixture)
{
    [Fact]
    public void ValidLoginLandsOffLoginAndShowsAppShell()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");

        var driver = fixture.NewDriver();
        var loginPage = new LoginPage(driver, SeleniumFixture.BaseUrl);
        loginPage.Goto();
        loginPage.Login(E2ESettings.Username, E2ESettings.Password);

        new WebDriverWait(driver, TimeSpan.FromSeconds(30))
            .Until(d => !d.Url.Contains("/login", StringComparison.Ordinal));
        PageAssertions.ShouldBeAuthenticated(driver);
        PageAssertions.ShouldNotShowError(driver);
        // App shell: the sidebar nav (MainLayout.razor) must be present after login.
        driver.FindElements(By.CssSelector("nav, aside")).Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void InvalidPasswordStaysOnLoginAndShowsError()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");

        var driver = fixture.NewDriver();
        var loginPage = new LoginPage(driver, SeleniumFixture.BaseUrl);
        loginPage.Goto();
        loginPage.Login(E2ESettings.Username, "DefinitelyWrongPassword1#");

        loginPage.WaitForErrorBanner().ShouldContain("ACCESS DENIED", Case.Insensitive);
        driver.Url.ShouldContain("/login");
    }

    [Fact]
    public void UnauthenticatedVisitToConnectionsRedirectsToLogin()
    {
        Assert.SkipUnless(E2ESettings.Enabled, "E2E_BASE_URL not set.");

        var driver = fixture.NewDriver();
        driver.Navigate().GoToUrl($"{SeleniumFixture.BaseUrl}/connections");

        new WebDriverWait(driver, TimeSpan.FromSeconds(30))
            .Until(d => d.Url.Contains("/login", StringComparison.Ordinal));
        PageAssertions.WaitForBlazor(driver);
        driver.FindElements(By.Id("username")).Count.ShouldBeGreaterThan(0);
    }
}
