namespace Reference.Ui.E2E.Infrastructure;

/// <summary>
/// Environment-driven E2E configuration. The suite targets a RUNNING reference-ui instance
/// (a preview slot or a locally launched UI) — it never starts the app itself.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><c>E2E_BASE_URL</c> — UI root, e.g. https://ui-ctc.preview.cyberdynedevelopment.dev.
///         When unset, the suite is SKIPPED (so it never breaks a plain unit-test run).</item>
///   <item><c>E2E_USERNAME</c> / <c>E2E_PASSWORD</c> — seeded login (default admin/AdminPassword1#).</item>
///   <item><c>E2E_HEADED</c> — set to <c>1</c> to watch the browser.</item>
///   <item><c>E2E_SQL_SERVER</c> — MsSql host the connection-wizard test targets. Required only by
///         that test; it throws rather than defaulting, since a wrong host would fail the wizard's
///         Test gate and read as a UI bug.</item>
/// </list>
/// </remarks>
public static class E2ESettings
{
    public static string? BaseUrl => Environment.GetEnvironmentVariable("E2E_BASE_URL")?.TrimEnd('/');

    public static string Username => Environment.GetEnvironmentVariable("E2E_USERNAME") ?? "admin";

    public static string Password => Environment.GetEnvironmentVariable("E2E_PASSWORD") ?? "AdminPassword1#";

    public static bool Headed => Environment.GetEnvironmentVariable("E2E_HEADED") == "1";

    /// <summary>
    /// MsSql host the connection-wizard happy-path test fills into the form. Deployment-specific,
    /// so it is never committed — and never defaulted, because a guessed host would fail the
    /// wizard's Test gate and be indistinguishable from the UI bug the test exists to catch.
    /// </summary>
    public static string SqlServer =>
        Environment.GetEnvironmentVariable("E2E_SQL_SERVER")
        ?? throw new InvalidOperationException(
            "E2E_SQL_SERVER is not set. The connection-wizard test configures a real MsSql target; "
            + "set it to the host the suite should point at.");

    /// <summary>True when an E2E target is configured; tests skip themselves otherwise.</summary>
    public static bool Enabled => !string.IsNullOrWhiteSpace(BaseUrl);
}
