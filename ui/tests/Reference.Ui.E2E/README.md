# Reference.Ui.E2E — real browser end-to-end tests

Playwright (.NET) tests that drive the **rendered UI in a real headless browser** and assert on the
**DOM the user sees** — not on API responses. Catches UI-rendering failures (e.g. "provider not
found / empty grid", a page that 500s, a circuit that crashes) that API/Newman tests are blind to.

## What it covers
- **`AllPagesRenderTests`** — every mapped, non-parameterised route loads authenticated, with no error
  boundary / failure banner and real rendered content (regression net for the static pages).
- **Per-area tests** (`Connections`, `DataSets`, `Users`/`Roles`) — the list paints real rows, search
  and type filters narrow it, the New button reaches the create form, and a row/card opens its detail
  page. This is the proven, non-destructive pattern to extend to every area.

## Run it (targets a RUNNING UI — a preview slot or a local instance)
```bash
# one-time: install the matching browser build.
dotnet build
# pwsh is required by Playwright's installer; if absent: dotnet tool install --global PowerShell
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
# NOTE: the browser build is pinned by the Microsoft.Playwright package version (currently v1155).
# A globally cached newer Chromium will NOT be used — install the pinned build with the line above.

export E2E_BASE_URL=https://ui-ctc.preview.cyberdynedevelopment.dev
export E2E_USERNAME=admin E2E_PASSWORD='AdminPassword1#'   # seeded dev creds
# export E2E_HEADED=1   # watch the browser
dotnet test
```
Without `E2E_BASE_URL` the whole suite **skips** (so it never breaks a plain unit-test run).

## Selectors
Each list page renders a different layout (Connections = card grid, DataSets = a divided-list card of
row buttons, Users = an HTML table, Roles = `rounded-xl` cards). The FDW UI ships **no `data-testid`**
yet, so every locator is written as `[data-testid=…]` **OR** the page's current semantic markup — the
suite is green against today's slot AND auto-upgrades to a stable test-id contract the moment the FDW
`*.UI.Pages`/`*.Components` expose `data-testid` (no test change needed). Adding those test-ids is the
durable follow-up; then fan the per-area coverage out to create→edit→delete→validate across all areas.

## Known-RED routes = real app bugs (NOT test defects)
The suite is deliberately red where the running app is genuinely broken. As of the last run against
`ui-ctc` (FDW `1.3.2-rc.1.1`):

| Red test(s) | Root cause (server-side) |
|---|---|
| `AllPagesRenderTests` `/messages` | `FractalDataWorks.Services.Messaging.Components.MessageProvider.LoadMessages` mutates component state **off the Blazor Dispatcher during prerender** → `InvalidOperationException` → HTTP 500 |
| `AllPagesRenderTests` `/access-requests` | Same off-Dispatcher state mutation in the access-request provider → HTTP 500 |
| `UsersTests` (table/search/new) | `FractalDataWorks.Services.Authorization.UI.Pages.Pages.Users` → `UserProvider.BuildRenderTree` throws `IndexOutOfRangeException` (empty-username avatar initial `Username[0]`) → circuit crash, table never paints |

Fix those in FDW (Dispatcher-marshal the provider loads via `InvokeAsync`; guard the avatar initial),
repack, redeploy the slot, and these go green with no test change.
