using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace Reference.Ui.Selenium.Infrastructure;

/// <summary>
/// Shared authenticated session for the Selenium suite. Logs in through the REAL UI login form
/// once, captures the auth cookies at driver level (they are HttpOnly — invisible to JS but fully
/// visible to WebDriver), and hands every test a fresh <see cref="ChromeDriver"/> pre-loaded with
/// those cookies — mirroring the Playwright suite's <c>storageState</c> pattern.
/// </summary>
public sealed class SeleniumFixture : IDisposable
{
    private readonly List<IWebDriver> _drivers = new();
    private readonly IReadOnlyList<Cookie> _authCookies;

    public static string BaseUrl => E2ESettings.BaseUrl
        ?? throw new InvalidOperationException("E2E_BASE_URL is not set.");

    public SeleniumFixture()
    {
        if (!E2ESettings.Enabled)
        {
            _authCookies = Array.Empty<Cookie>(); // tests self-skip; don't spin up a browser for nothing
            return;
        }

        using var driver = CreateDriver();
        driver.Navigate().GoToUrl($"{BaseUrl}/login");
        PageAssertions.WaitForBlazor(driver);

        // Why: the SSR login form uses stable element ids (#username/#password) — the visible labels
        // are themed copy ("Identity"/"Passcode"), so a label-text locator is brittle. Bind to the ids.
        driver.FindElement(By.Id("username")).SendKeys(E2ESettings.Username);
        driver.FindElement(By.Id("password")).SendKeys(E2ESettings.Password);
        driver.FindElement(By.CssSelector("button[type=submit]")).Click();

        // Landed off /login = authenticated.
        new WebDriverWait(driver, TimeSpan.FromSeconds(30))
            .Until(d => !d.Url.Contains("/login", StringComparison.Ordinal));

        _authCookies = driver.Manage().Cookies.AllCookies.ToArray();
    }

    /// <summary>A fresh browser already carrying the captured auth cookies, parked on the app root.</summary>
    public IWebDriver NewAuthenticatedDriver()
    {
        var driver = NewDriver();
        // Why: WebDriver only accepts cookies for the origin currently loaded, so we must visit the
        // site first. /login is the one route that renders without auth (no redirect loop).
        driver.Navigate().GoToUrl($"{BaseUrl}/login");
        foreach (var cookie in _authCookies)
            driver.Manage().Cookies.AddCookie(cookie);
        driver.Navigate().GoToUrl(BaseUrl);
        PageAssertions.WaitForBlazor(driver);
        return driver;
    }

    /// <summary>A fresh UNauthenticated browser (for login-form and redirect tests).</summary>
    public IWebDriver NewDriver()
    {
        var driver = CreateDriver();
        _drivers.Add(driver);
        return driver;
    }

    private static ChromeDriver CreateDriver()
    {
        var options = new ChromeOptions();
        options.AddArgument("--window-size=1600,1000");
        if (!E2ESettings.Headed)
        {
            options.AddArgument("--headless=new");
            if (OperatingSystem.IsLinux())
            {
                // Why: headless Chrome on CI-ish Linux boxes crashes without these — /dev/shm is
                // tiny in containers and the sandbox needs user namespaces that may be disabled.
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
            }
        }
        if (!string.IsNullOrWhiteSpace(E2ESettings.BrowserBinary))
            options.BinaryLocation = E2ESettings.BrowserBinary;
        // When no binary is given, Selenium Manager provisions Chrome for Testing + chromedriver.
        return new ChromeDriver(options);
    }

    public void Dispose()
    {
        foreach (var driver in _drivers)
            driver.Quit(); // Quit ends the session AND stops the chromedriver process
    }
}
