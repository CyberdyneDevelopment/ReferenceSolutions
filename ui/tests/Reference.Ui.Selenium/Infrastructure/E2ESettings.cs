namespace Reference.Ui.Selenium.Infrastructure;

/// <summary>
/// Environment-driven E2E configuration. The suite targets a RUNNING reference-ui instance
/// (a preview slot or a locally launched UI) — it never starts the app itself. Same env
/// contract as the Playwright suite (Reference.Ui.E2E) so one environment drives both.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><c>E2E_BASE_URL</c> — UI root, e.g. https://ui-selenium.preview.cyberdynedevelopment.dev.
///         When unset, the suite is SKIPPED (so it never breaks a plain unit-test run).</item>
///   <item><c>E2E_USERNAME</c> / <c>E2E_PASSWORD</c> — seeded login (default admin/AdminPassword1#).</item>
///   <item><c>E2E_HEADED</c> — set to <c>1</c> to watch the browser.</item>
///   <item><c>E2E_BROWSER_BINARY</c> — optional path to a specific Chrome/Chromium binary.
///         When unset, Selenium Manager provisions browser + driver automatically.</item>
/// </list>
/// </remarks>
public static class E2ESettings
{
    public static string? BaseUrl => Environment.GetEnvironmentVariable("E2E_BASE_URL")?.TrimEnd('/');

    public static string Username => Environment.GetEnvironmentVariable("E2E_USERNAME") ?? "admin";

    public static string Password => Environment.GetEnvironmentVariable("E2E_PASSWORD") ?? "AdminPassword1#";

    public static bool Headed => string.Equals(Environment.GetEnvironmentVariable("E2E_HEADED"), "1", StringComparison.Ordinal);

    public static string? BrowserBinary => Environment.GetEnvironmentVariable("E2E_BROWSER_BINARY");

    /// <summary>True when an E2E target is configured; tests skip themselves otherwise.</summary>
    public static bool Enabled => !string.IsNullOrWhiteSpace(BaseUrl);
}
