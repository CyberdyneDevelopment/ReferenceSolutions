using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reference.Ui.Selenium.Infrastructure;

namespace Reference.Ui.Selenium.Pages;

/// <summary>
/// Page Object for the SSR login form (Components/Pages/Login.razor). Stable ids
/// <c>#username</c>/<c>#password</c>, plain form POST to <c>/auth/login</c>; a failed login
/// redirects back to <c>/login?error=invalid-credentials</c> which renders
/// <c>&lt;div class="login-err"&gt;ACCESS DENIED: …&lt;/div&gt;</c>.
/// </summary>
public sealed class LoginPage(IWebDriver driver, string baseUrl)
{
    public void Goto()
    {
        driver.Navigate().GoToUrl($"{baseUrl}/login");
        PageAssertions.WaitForBlazor(driver);
    }

    /// <summary>Fills the form and submits. Does NOT wait for an outcome — callers assert success or error.</summary>
    public void Login(string username, string password)
    {
        driver.FindElement(By.Id("username")).SendKeys(username);
        driver.FindElement(By.Id("password")).SendKeys(password);
        driver.FindElement(By.CssSelector("button[type=submit]")).Click();
    }

    /// <summary>The visible error banner text, waited for — empty string never occurs (throws on timeout).</summary>
    public string WaitForErrorBanner(int timeoutSeconds = 15)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
        // Why: the page re-renders when the error query-string lands, so an element found one poll
        // ago can go stale before .Text is read — staleness here means "poll again", not "fail".
        wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException), typeof(NoSuchElementException));
        return wait.Until(d =>
        {
            var banners = d.FindElements(By.CssSelector(".login-err"));
            return banners.Count > 0 && banners[0].Displayed ? banners[0].Text : null;
        })!;
    }
}
