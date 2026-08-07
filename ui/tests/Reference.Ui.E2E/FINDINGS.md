# Reference UI — Test Coverage & Findings (FDW 1.3.2-rc.1.3, slot `ui-ctc`)

The UI is tested at **two points** (headless design = render+logic separable from end-to-end):
- **bUnit** (`Reference.Management.UI.Tests`) — render + provider-logic, hermetic (Moq clients, no slot):
  **842 cases, 837 pass / 5 RED**, ~7s. Every branch of all 144 components in isolation.
- **Playwright E2E** (`Reference.Ui.E2E`) — real browser against the live slot:
  **311 cases, 260 pass / 43 RED / 8 skip**, ~18min. Every *reachable* behavior end-to-end;
  physically-unreachable branches (need an injected internal error, a missing endpoint, or a
  page that crashes) are covered by bUnit only and labelled as such in each test class.

Convention: **red-on-bug** — tests assert CORRECT behavior; a RED test = a confirmed bug (root cause
inline). Fixtures are test-owned (unique-prefix, self-cleaning); `ApiSeeder` seeds via the API so
list/filter/edit/delete are deterministic even where UI-create is broken.

Element denominator (coverage target): 298 inputs · 753 actions · 1,170 render branches · 144 components.

## Headline
The UI is **read-functional but create/mutation is broken across most domains**. Lists, detail
views, graphs, and navigation largely work; **creating or editing records fails in nearly every area.**
The comprehensive pass also pinned several root causes and found new defects (see table): the Schedule
list envelope-deserialize mismatch, Projects/Orchestration being entirely unrouted (404), Role
permission-matrix + Notification-preference saves not persisting, DataSet annotation tags dropped, and
the Glossary `Term`→`Name` mapping gap.

---

## A. Create / mutation broken (CODE)
| # | Area | Bug | Root cause |
|---|------|-----|-----------|
| 1 | DataSets | `/datasets/new` + `/{name}/edit` **crash the Blazor circuit** ("error applying batch 2" → terminated); every @onclick dead → can't create/edit a dataset | suspect `OptionPicker<IDataSetCategory>` (StaticOptions=`DataSetCategories.All()`) on `DataSetWizard.razor` step 0 |
| 2 | Schedules | Create **persists** (verified rows in `sched.Schedule` + `GET /api/v1/schedules` returns them) but list **always shows "No schedules configured"** | **Root cause isolated:** the API returns a paged envelope `{items:[…],totalCount}` but `ScheduleHttpClient.List` deserializes into `Get<IReadOnlyList<ScheduleInfoDto>>` (a bare array) which can't unwrap the envelope → empty result → empty-state (page never renders `ctx.ErrorMessage`). `PipelineHttpClient.List` uses `GetList` (unwraps envelope) and is **not** affected — pipelines render fine. Fix: switch the schedule client to `GetList`. |
| 3 | Calculations | Create/list **silently fail** (stays on `/calculations/new`, masked banner) | route mismatch: UI `CalculationApiClient` calls `calculations`/`{id}`; Reference.Api only exposes `calculation-entities/*` |
| 4 | Quality | Rule create → **HTTP 400**; rules can't be made from `/quality/rules` | form sends only Name/Description; server `CreateQualityRuleRequest` requires `DataSetName`+`RuleType` (no fields for them) |
| 5 | Glossary | Create → **HTTP 500** | base `CreateGlossaryTermEndpoint` (Catalog) fails server-side |
| 6 | Promotions | Create succeeds but **Name renders blank** in the list | API layer (`ListPromotionsEndpointBase`/`IPromotionService`) returns empty Name; UI binds it correctly |
| 7 | Settings | General **doesn't round-trip** (fields hardcoded, not loaded from persisted); Notifications-tab Save is a **dead control** (no `@onclick`, Settings.razor:145); Security Save **fails** (backend rejects `Enable2FA`) | per-row, cited |
| 8 | Configuration | `/configuration` category sidebar **empty** ("No configuration types found") → CRUD unreachable; `/configuration/issues` **Validate inert** | provider auto-loads `LoadInstances`, never a types/`GetRootTypes` load; validate never fires |
| 9 | Connections | create wizard's **type dropdown never loads** ("Loading connection types…" forever) → UI-driven connection create is impossible | **CONFIRMED (not load-induced):** `ConnectionWizardContext` loads types from `configuration/types/Connection` which **404s**; the working route is `connections/types` (returns 200 with all types). Wrong endpoint → `ConnectionTypes` stays empty → the type `<select>` never renders. |
| 9c | Connections | **list search/filter/sort operate only on the first 100 of 215 rows** — the client loads `take=100` (`hasMore=true`) and does search/filter/sort client-side over that page with no server re-query; rows past page 1 are unreachable from the UI | client-side paging over a server-truncated page; needs server-side query or full fetch |
| 9d | SecretManagers | UI create **omits the required `Configuration` object** → POST 400, no card created | the create form never sets `Configuration`, which the server validator requires (`Configuration object is required`) |
| 9e | DataStores | wizard **can't advance past the Store-Type step** → create + edit both blocked (Save is on the final step) | store-type/capabilities never yield a selection that enables Next |

| 9b | Projects / Orchestration | **Entire `/projects*` and `/orchestration*` feature is unreachable** — every route returns **HTTP 404** and renders a blank `<html><body></body></html>` shell (vs routed `/pipelines` → 302→login). All bUnit-covered branches (list/tree, search, create, edit, delete, run, policy form, stage designer, execution monitor, Ordinal/AllowResume) are bUnit-only. | The `FractalDataWorks.Services.Etl.Projects.UI.Pages` routable pages are referenced by reference-ui but **not registered in the deployed UI's router**. Data path is dead anyway: UI clients target `projects`/`nodes`, which reference-api 404s (node endpoints live only in reference-etl as `etl/nodes…`; no Project CRUD endpoint exists anywhere). |

## B. Page-level crashes (CODE)
| # | Route | Bug | Root cause |
|---|-------|-----|-----------|
| 10 | `/messages`, `/messages/{id}` | **HTTP 500**, blank page | `MessageProvider` auto-loads in `OnInitializedAsync` + `StateHasChanged()` **off the Blazor Dispatcher during prerender** (every other provider uses `OnAfterRenderAsync(firstRender)`) |
| 11 | `/access-requests` | **HTTP 500** | same MessageProvider off-Dispatcher pattern (AutoLoadAccessRequests) |
| 12 | `/users` | **IndexOutOfRangeException** → table never paints (stuck on loading dots) | `Users.razor` `@user.Username[0]`; `GET /api/v1/users` returns a row with `username:""` → throws in `BuildRenderTree`. Fix: guard the avatar initial |

## C. Filters / search / sort broken (CODE)
| # | Area | Bug |
|---|------|-----|
| 13 | Audit | filters inert — Apply calls `ctx.OnRefresh` (reads provider fields) not `OnFilterChanged`; selections never reach the query |
| 14 | Glossary | search ignores `q` — `GET /catalog/glossary/search?q=` returns the full list |
| 15 | Pipelines/Schedules/Calculations | **no search box rendered** though providers carry `Filtered*`/`OnSearchChanged` |
| 16 | Connections | sort dropdown **no-op** — changing the sort `<select>` doesn't re-render the displayed order (CONFIRMED on a quiet slot); also the empty-state checks the full loaded list not the filtered set, so a no-match search shows a blank grid with no message |

## D. Data-Preview / viz (CODE)
| # | Bug |
|---|-----|
| 17 | Data-Preview Table mode **can't load connections** (`SchemaProvider: Failed to load connections list`) → Table preview unusable |
| 18 | Data-Preview mode toggle is **one-way** — DataSet→Table never switches back (`SetTableMode` issues no inverse mode-change) |
| 19 | Data-Preview **"Add Filter" is inert** — appends no filter row in either mode; `OnExecute` also never copies `ctx.Filters` |
| 20 | Quality dashboard load fails (`GET quality/dashboard` non-success → error banner + empty-state) |
| 21 | Quality has **no edit UI** despite PUT plumbing (rows expose only Execute/Delete) |
| 22 | Mapper "Save Mappings" **doesn't persist** (logs only); `OnValidate` unwired |
| 23 | Dataflow **never renders** provider `ErrorMessage` |
| 24 | Lineage **silently swallows** entity-name/expand failures |
| 25 | Glossary persisted rows render a **blank Term** (`<h3></h3>`) — Term not stored/returned |

## E. Environmental / config (NOT necessarily code bugs)
| # | Item | Note |
|---|------|------|
| 26 | API `GET /api/v1/session-state` → **500** (`ListSessionStateKeys` fails) — breaks list-filter persistence | api-side; likely OpsDb/session config |
| 27 | API `GET /api/v1/health/system` → **500** ("BaseAddress must be set") | health aggregator calls **etl/scheduler, not deployed** on this slot |
| 28 | `/schema` `GetCapableConnections()` returns **zero** on the fresh slot → discover/tree/column-detail unreachable | data/seed state |
| 29 | api-keys token/agent-key routes (`users/me/tokens`, `agent-keys`) → **404** on this API build | possibly API build/feature flag |

---

## Suite conventions (to normalize)
- **red-on-bug** (Connections, Ops, Users, DataSets groups): the test asserts *correct* behavior and is **RED** on the bug.
- **characterization** (Quality, Pipelines groups, tagged `[Trait("Status","Red")]`): the test asserts the *current broken* behavior and is **GREEN now**, flipping when fixed.
- TODO: normalize to red-on-bug so failure count = broken-feature count.

## Fixtures
Test-owned: each test creates uniquely-prefixed entities and self-cleans (`finally`). `ApiSeeder` seeds
via the API (OpenIddict password grant) for areas whose UI create is broken; the create-flow tests
themselves drive the UI. Areas with no working create path (DataSets, Schedules, Calculations, Quality,
Glossary) document the blocker instead of seeding through the UI.
