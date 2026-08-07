# Reference.Ui.Selenium — browser E2E suite (Selenium)

Drives the **real reference-ui in a real Chrome browser** with Selenium WebDriver. Companion to
the Playwright suite (`Reference.Ui.E2E`) — same env contract, same RED-on-bug convention, same
route denominator. Use this suite when you want WebDriver semantics or to run from a machine
where Selenium is the house tool.

## Requirements (any computer)

- .NET 10 SDK
- Chrome (or any Chromium): **Selenium Manager provisions the matching chromedriver
  automatically** — no driver install, no PATH setup. If no Chrome is installed it downloads
  Chrome for Testing on first run.

## Environment contract

| Variable | Meaning | Default |
|----------|---------|---------|
| `E2E_BASE_URL` | UI root to test. **Unset ⇒ the whole suite skips** (safe in plain `dotnet test`). | — |
| `E2E_USERNAME` | Seeded login | `admin` |
| `E2E_PASSWORD` | Seeded password | `AdminPassword1#` |
| `E2E_HEADED` | `1` = watch the browser | headless |
| `E2E_BROWSER_BINARY` | Optional path to a specific Chrome/Chromium binary | Selenium Manager |

## Run it

```bash
# against the live preview slot, headless
E2E_BASE_URL=https://ui-selenium.preview.cyberdynedevelopment.dev \
  dotnet test public/tests/Reference.Ui.Selenium/Reference.Ui.Selenium.csproj -c Release

# watch the browser
E2E_HEADED=1 E2E_BASE_URL=https://ui-selenium.preview.cyberdynedevelopment.dev \
  dotnet test public/tests/Reference.Ui.Selenium/Reference.Ui.Selenium.csproj -c Release

# one class / one route
... --filter "FullyQualifiedName~LoginTests"
... --filter "DisplayName~/connections"
```

## Architecture

- `Infrastructure/SeleniumFixture.cs` — logs in ONCE through the real form, captures the chunked
  auth cookies at driver level, and hands each test a fresh `ChromeDriver` pre-loaded with them
  (the Playwright `storageState` pattern). Note: the auth cookie is 5 chunks ≈ 20 KB — fine in
  every browser, but **curl silently drops the >4 KB chunks**, so never "verify" login with curl.
- `Infrastructure/PageAssertions.cs` — `WaitForBlazor` (readyState + circuit-rendered content),
  `ShouldBeAuthenticated`, `ShouldNotShowError` (error-boundary/banner markers).
- `Pages/` — `LoginPage` (`#username`/`#password`), `ListPage` (data-testid-first locators with
  semantic fallbacks; `Search()` does fill+Tab because FDW inputs `@bind` on change/blur).
- `Tests/` — `LoginTests`, `RouteRenderTests` (42-route authenticated sweep, kept in lockstep
  with the Playwright `AllPagesRenderTests.Routes`), `ConnectionsInteractionTests` (read-only
  list interactions), `NavigationTests` (sidebar, logout).

## Conventions

- **RED-on-bug**: tests assert *correct* behavior. A genuinely broken page keeps its test red —
  red count = broken-feature count. Never write characterization tests that go green on a bug.
- **Read-only against shared slots**: interaction tests never submit mutations.
- Every test opens with `Assert.SkipUnless(E2ESettings.Enabled, ...)`.

## Current results — 2026-07-04, slot `ui-selenium` (develop + RUI-47/RUI-49 fixes, FDW 1.0.1-rc.1, api-fr backend)

**52/52 green.** Getting here surfaced and fixed two real app defects on develop:

1. **RUI-47** — UI host crashed at startup (PlatformServices sweep registered the OpenIddict
   auth-server hosted services; missing `authz:SystemRoleMapping` config).
2. **RUI-49** — ambiguous route `/settings` (app-local page vs packaged `Fdw.UI.Pages.Settings`)
   **killed every interactive circuit app-wide**: pages showed only their SSR prerender, all lists
   rendered empty, and no API call ever left the UI. Fixed by deleting the app-local page — the
   packaged screen owns `/settings`.

**Known cross-suite caveat**: the Playwright sweep currently reports 5 of these routes red
(`/schema`, `/schema/tables/new`, `/calculations/new`, `/connectors`, `/configuration/issues`).
That is a Playwright-side timing artifact: `NetworkIdle` fires before the Blazor circuit paints
(SignalR traffic doesn't count as network), so it reads an empty `<main>` on slow circuit-rendered
pages. This suite's `WaitForBlazor` waits for content stability instead, and those pages then
assert non-empty main content and pass — the content genuinely arrives, just after network-idle.
