using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Reference.Ui.E2E.Infrastructure;

namespace Reference.Ui.E2E.Pages;

/// <summary>
/// Test-owned fixture seeder. The slot DB is polluted with hundreds of ambient junk rows from other
/// suites, so "≥1" assertions are worthless — every read/search/filter/detail test must create its OWN
/// known entities (unique <c>e2e-…-{8hex}</c> prefix) and assert EXACT counts scoped to that prefix.
/// This talks to the SAME reference-api the UI uses (the <c>ui-…</c> slot proxies to the <c>api-…</c>
/// slot on the same VM), reached over public HTTPS by host-substituting <c>ui-</c> → <c>api-</c> in
/// <see cref="E2ESettings.BaseUrl"/>. It is a SEEDING channel only — every assertion still runs against
/// the rendered browser DOM, never against these responses.
/// </summary>
public sealed class ApiSeeder : IAsyncDisposable
{
    private readonly HttpClient _http;
    private readonly List<Func<Task>> _cleanups = new();

    private ApiSeeder(HttpClient http) => _http = http;

    /// <summary>The reference-api root, derived from the UI base URL (<c>ui-*</c> → <c>api-*</c>).</summary>
    public static string ApiBaseUrl
    {
        get
        {
            var ui = E2ESettings.BaseUrl ?? throw new InvalidOperationException("E2E_BASE_URL not set.");
            var host = new Uri(ui).Host; // e.g. ui-ctc.preview.cyberdynedevelopment.dev
            // Why: the UI slot 'ui-<x>' is wired to the API slot 'api-<x>' on the same VM; the public
            // host follows the same naming, so swap the leading 'ui-' segment for 'api-'.
            if (!host.StartsWith("ui-", StringComparison.Ordinal))
                throw new InvalidOperationException($"Cannot derive API host from UI host '{host}'.");
            return $"https://api-{host.Substring(3)}";
        }
    }

    // Why: every reference-api endpoint is mounted under a global "/api/v1" version prefix (probed live:
    // GET /datasets → 404, GET /api/v1/datasets → 200). The existing user/role/access-request helpers above
    // already encode "api/v1/…" inline; the data-domain helpers below reuse this constant so the prefix is
    // declared once.
    private const string V1 = "api/v1";

    /// <summary>Logs in via OpenIddict password grant and returns a ready, bearer-authorized seeder.</summary>
    public static async Task<ApiSeeder> CreateAsync()
    {
        var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
        var http = new HttpClient(handler) { BaseAddress = new Uri(ApiBaseUrl + "/") };

        using var resp = await http.PostAsync("connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = E2ESettings.Username,
            ["password"] = E2ESettings.Password,
            ["client_id"] = "reference-client",
            ["scope"] = "fdw.api offline_access",
        }));
        resp.EnsureSuccessStatusCode();
        var token = (await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync()))
            .RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("No access_token in token response.");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new ApiSeeder(http);
    }

    /// <summary>A collision-proof per-test prefix: <c>{kind}-{8hex}</c>.</summary>
    public static string NewPrefix(string kind) => $"{kind}-{Guid.NewGuid():N}".Substring(0, kind.Length + 1 + 8);

    /// <summary>
    /// A collision-proof per-test USERNAME prefix using only underscores: <c>{kind}_{8hex}</c>. The API's
    /// username validator requires a leading letter and only letters/digits/underscores (no hyphens), so
    /// user fixtures cannot reuse the hyphenated <see cref="NewPrefix"/>.
    /// </summary>
    public static string NewUserPrefix(string kind) => $"{kind}_{Guid.NewGuid():N}".Substring(0, kind.Length + 1 + 8);

    // ---- Users -----------------------------------------------------------------------------------

    /// <summary>Creates a user and registers its deletion. Returns the username.</summary>
    public async Task<string> CreateUserAsync(string username, string? email = null, IEnumerable<string>? roles = null, bool active = true)
    {
        var body = new Dictionary<string, object?>
        {
            ["username"] = username,
            ["password"] = "Passw0rd123!",
            ["email"] = email,
            ["roles"] = roles?.ToArray() ?? Array.Empty<string>(),
            ["isActive"] = active,
        };
        (await _http.PostAsJsonAsync("api/v1/users", body)).EnsureSuccessStatusCode();
        _cleanups.Add(() => DeleteUserAsync(username));
        return username;
    }

    public async Task DeleteUserAsync(string username)
    {
        using var _ = await _http.DeleteAsync($"api/v1/users/{Uri.EscapeDataString(username)}");
    }

    // ---- Roles -----------------------------------------------------------------------------------

    public async Task<string> CreateRoleAsync(string name, string? description = null)
    {
        (await _http.PostAsJsonAsync("api/v1/roles", new Dictionary<string, object?>
        {
            ["name"] = name,
            ["description"] = description,
        })).EnsureSuccessStatusCode();
        _cleanups.Add(() => DeleteRoleAsync(name));
        return name;
    }

    public async Task DeleteRoleAsync(string name)
    {
        using var _ = await _http.DeleteAsync($"api/v1/roles/{Uri.EscapeDataString(name)}");
    }

    /// <summary>Register a role created THROUGH THE UI for teardown (so the API still cleans it up).</summary>
    public void TrackRoleForCleanup(string name) => _cleanups.Add(() => DeleteRoleAsync(name));

    /// <summary>Register a user created THROUGH THE UI for teardown.</summary>
    public void TrackUserForCleanup(string username) => _cleanups.Add(() => DeleteUserAsync(username));

    // ---- Access requests -------------------------------------------------------------------------

    /// <summary>Creates a pending access request and returns its id.</summary>
    public async Task<string> CreateAccessRequestAsync(string resource, string permission, string? justification = null)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/access-requests", new Dictionary<string, object?>
        {
            ["requestedResource"] = resource,
            ["requestedPermission"] = permission,
            ["justification"] = justification,
        });
        resp.EnsureSuccessStatusCode();
        return (await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync()))
            .RootElement.GetProperty("id").GetString() ?? string.Empty;
    }

    // ---- DataSets --------------------------------------------------------------------------------

    /// <summary>
    /// Creates a DataSet via <c>POST /api/v1/datasets</c> and registers its deletion. Returns the name.
    /// The DataSet create wizard in the UI crashes its circuit on connect (confirmed app bug), so the ONLY
    /// way to own a deterministic DataSet fixture is this API seed — it lets the list/search/filter/detail
    /// E2E tests assert against KNOWN rows instead of ambient junk.
    /// </summary>
    public async Task<string> CreateDataSetAsync(string name, string? category = null, string? description = null)
    {
        var body = new Dictionary<string, object?>
        {
            ["name"] = name,
            ["description"] = description,
            ["category"] = category,
            ["version"] = "1.0",
            ["recordTypeName"] = name,
            ["keyFields"] = Array.Empty<string>(),
            ["dataSetType"] = "Standard",
        };
        (await _http.PostAsJsonAsync($"{V1}/datasets", body)).EnsureSuccessStatusCode();
        _cleanups.Add(() => DeleteDataSetAsync(name));
        return name;
    }

    public async Task DeleteDataSetAsync(string name)
    {
        using var _ = await _http.DeleteAsync($"{V1}/datasets/{Uri.EscapeDataString(name)}");
    }

    // ---- DataSet annotations ---------------------------------------------------------------------

    /// <summary>
    /// Creates a DataSet annotation via <c>POST /api/v1/catalog/datasets/{name}/annotations</c>. Annotations
    /// are version-on-write rows (no hard delete needed for cleanup — they're scoped to the parent DataSet,
    /// which the DataSet cleanup removes). Returns nothing; the parent DataSet teardown is sufficient.
    /// </summary>
    public async Task CreateAnnotationAsync(string dataSetName, string? owner, string? steward, string? classification, IEnumerable<string>? tags = null)
    {
        var body = new Dictionary<string, object?>
        {
            ["dataSetName"] = dataSetName,
            ["owner"] = owner,
            ["steward"] = steward,
            ["classification"] = classification,
            ["tags"] = tags?.ToArray() ?? Array.Empty<string>(),
        };
        (await _http.PostAsJsonAsync($"{V1}/catalog/datasets/{Uri.EscapeDataString(dataSetName)}/annotations", body))
            .EnsureSuccessStatusCode();
    }

    // ---- Calculation entities --------------------------------------------------------------------

    /// <summary>
    /// Creates a calculation entity via <c>POST /api/v1/calculation-entities</c> (the REAL calc CRUD route —
    /// the UI mistakenly targets <c>calculations</c>, which 404s) and registers its deletion. Returns the id.
    /// Seeding here proves the API holds the row even though the UI's <c>/calculations</c> list can't read it
    /// (route-mismatch bug) — the E2E asserts that gap rather than papering over it.
    /// </summary>
    public async Task<string> CreateCalculationEntityAsync(string name, string outputDataSetName, string? description = null)
    {
        var body = new Dictionary<string, object?>
        {
            ["name"] = name,
            ["description"] = description,
            ["calculationEntityType"] = "Formula",
            ["inputs"] = Array.Empty<object>(),
            ["outputDataSetName"] = outputDataSetName,
            ["resultFieldName"] = "result",
            ["resultDataTypeName"] = "Decimal",
        };
        var resp = await _http.PostAsJsonAsync($"{V1}/calculation-entities", body);
        resp.EnsureSuccessStatusCode();
        var id = (await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync()))
            .RootElement.GetProperty("id").GetString() ?? string.Empty;
        _cleanups.Add(() => DeleteCalculationEntityAsync(id));
        return id;
    }

    public async Task DeleteCalculationEntityAsync(string id)
    {
        using var _ = await _http.DeleteAsync($"{V1}/calculation-entities/{Uri.EscapeDataString(id)}");
    }

    /// <summary>
    /// Best-effort cleanup for a calculation entity created through the UI (where the id isn't captured):
    /// list the entities, match by name, and delete. Tolerant of the {items:[...]} envelope or a bare array.
    /// </summary>
    public async Task DeleteCalculationEntityByNameAsync(string name)
    {
        using var resp = await _http.GetAsync($"{V1}/calculation-entities");
        if (!resp.IsSuccessStatusCode) return;
        var root = (await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync())).RootElement;
        var items = root.ValueKind == JsonValueKind.Array ? root
            : root.TryGetProperty("items", out var it) ? it : default;
        if (items.ValueKind != JsonValueKind.Array) return;
        foreach (var e in items.EnumerateArray())
        {
            if (e.TryGetProperty("name", out var n)
                && string.Equals(n.GetString(), name, StringComparison.OrdinalIgnoreCase)
                && e.TryGetProperty("id", out var id))
            {
                await DeleteCalculationEntityAsync(id.GetString()!);
            }
        }
    }

    // ---- Quality rules ---------------------------------------------------------------------------

    /// <summary>
    /// Seeds a quality rule via POST <c>quality/rules</c> with the SERVER-correct payload (DataSetName +
    /// RuleType are required by FluentValidation; the broken UI create form omits them). Returns the
    /// created rule's <c>Id</c> (string) for later UI delete/execute, and registers API teardown.
    /// </summary>
    /// <param name="dataSetName">DataSet the rule applies to (required server-side).</param>
    /// <param name="ruleType">Rule type, e.g. <c>NotNull</c> (required server-side).</param>
    /// <param name="description">
    /// Human-readable description. The UI rules table binds the summary DTO's <c>Description</c> column to
    /// this, so a unique-prefixed description is how a seeded rule is found in the rendered list (the
    /// <c>Name</c> column is BLANK — the server's QualityRuleDto carries no Name, a documented defect).
    /// </param>
    /// <param name="fieldName">Optional field the rule validates.</param>
    /// <param name="isEnabled">Whether the rule renders the Enabled vs Disabled badge.</param>
    public async Task<string> CreateQualityRuleAsync(
        string dataSetName,
        string ruleType,
        string description,
        string? fieldName = null,
        bool isEnabled = true)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/quality/rules", new Dictionary<string, object?>
        {
            ["dataSetName"] = dataSetName,
            ["fieldName"] = fieldName,
            ["ruleType"] = ruleType,
            ["severity"] = "Error",
            ["isEnabled"] = isEnabled,
            ["description"] = description,
        });
        resp.EnsureSuccessStatusCode();
        var id = (await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync()))
            .RootElement.GetProperty("id").GetString() ?? string.Empty;
        _cleanups.Add(() => DeleteQualityRuleAsync(id));
        return id;
    }

    public async Task DeleteQualityRuleAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        using var _ = await _http.DeleteAsync($"api/v1/quality/rules/{Uri.EscapeDataString(id)}");
    }

    // ---- Promotions ------------------------------------------------------------------------------

    /// <summary>
    /// Seeds a pending promotion request via POST <c>promotion/requests</c> (the same endpoint the UI
    /// create form calls). Returns the created request's <c>id</c> (string). No teardown is registered:
    /// promotions are removed by the approve/reject terminal transition the tests drive themselves.
    /// NOTE: the server <c>CreatePromotionPayload</c> has NO Name field — only Source/Target/RequestedBy
    /// — which is why the list's Name column renders blank (documented defect).
    /// </summary>
    public async Task<string> CreatePromotionAsync(string source, string target, string? requestedBy = null)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/promotion/requests", new Dictionary<string, object?>
        {
            ["sourceEnvironment"] = source,
            ["targetEnvironment"] = target,
            ["requestedBy"] = requestedBy ?? "e2e-seeder",
            ["items"] = Array.Empty<object>(),
        });
        resp.EnsureSuccessStatusCode();
        return (await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync()))
            .RootElement.TryGetProperty("id", out var idEl)
            ? (idEl.ValueKind == JsonValueKind.String ? idEl.GetString() ?? string.Empty : idEl.ToString())
            : string.Empty;
    }

    // ---- Schedules ------------------------------------------------------------------------------

    /// <summary>
    /// Creates a schedule through the SAME reference-api the UI proxies to and registers its deletion.
    /// Returns the schedule name. Used to prove the UI list-read bug deterministically (the row exists
    /// in ConfigurationDb / the API list envelope but the UI's <c>Get&lt;IReadOnlyList&lt;T&gt;&gt;</c>
    /// cannot unwrap the <c>{items:[…]}</c> envelope, so it never renders).
    /// </summary>
    public async Task<string> CreateScheduleAsync(
        string name,
        string pipelineName = "Ingest-EspnNflTeams",
        string schedulerType = "Cron",
        string? cronExpression = "0 */5 * * *",
        int? intervalSeconds = null,
        string? eventName = null,
        string timeZoneId = "UTC",
        bool isEnabled = true)
    {
        var body = new Dictionary<string, object?>
        {
            ["name"] = name,
            ["pipelineName"] = pipelineName,
            ["schedulerType"] = schedulerType,
            ["cronExpression"] = cronExpression,
            ["intervalSeconds"] = intervalSeconds,
            ["eventName"] = eventName,
            ["timeZoneId"] = timeZoneId,
            ["isEnabled"] = isEnabled,
        };
        (await _http.PostAsJsonAsync("api/v1/schedules", body)).EnsureSuccessStatusCode();
        _cleanups.Add(() => DeleteScheduleAsync(name));
        return name;
    }

    public async Task DeleteScheduleAsync(string name)
    {
        using var _ = await _http.DeleteAsync($"api/v1/schedules/{Uri.EscapeDataString(name)}");
    }

    /// <summary>Register a schedule created THROUGH THE UI for teardown (so the API still cleans it up
    /// even though the broken UI list can't drive an in-UI delete).</summary>
    public void TrackScheduleForCleanup(string name) => _cleanups.Add(() => DeleteScheduleAsync(name));

    /// <summary>True when the API list envelope contains a schedule with the given name (read-back check
    /// that bypasses the broken UI list, used only to confirm the WRITE path succeeded).</summary>
    public async Task<bool> ScheduleExistsAsync(string name)
    {
        using var resp = await _http.GetAsync("api/v1/schedules");
        resp.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        var items = doc.RootElement.TryGetProperty("items", out var i) ? i : doc.RootElement;
        foreach (var el in items.EnumerateArray())
            if (el.TryGetProperty("name", out var n) &&
                string.Equals(n.GetString(), name, StringComparison.Ordinal))
                return true;
        return false;
    }

    /// <summary>The first pipeline name the API list exposes (the UI run-action targets a real pipeline).
    /// Returns null when the list is empty.</summary>
    public async Task<string?> FirstPipelineNameAsync()
    {
        var names = await PipelineNamesAsync();
        return names.Count > 0 ? names[0] : null;
    }

    /// <summary>The number of pipelines the API list returns — the exact count the healthy UI list must
    /// render (the pipeline client unwraps the paged envelope, so list rows == API items).</summary>
    public async Task<int> PipelineCountAsync() => (await PipelineNamesAsync()).Count;

    private async Task<IReadOnlyList<string>> PipelineNamesAsync()
    {
        using var resp = await _http.GetAsync("api/v1/pipelines");
        resp.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        var items = doc.RootElement.TryGetProperty("items", out var i) ? i : doc.RootElement;
        var names = new List<string>();
        foreach (var el in items.EnumerateArray())
            if (el.TryGetProperty("name", out var n) && n.GetString() is { } s)
                names.Add(s);
        return names;
    }

    // ---- Route probes ---------------------------------------------------------------------------

    /// <summary>True when the reference-api serves the given <c>api/v1/{route}</c> list path (any non-404
    /// status). Used to prove a feature's data path is dead end-to-end when the UI client targets a route
    /// the API never registered (e.g. <c>projects</c>, <c>nodes</c>).</summary>
    public async Task<bool> RouteExistsAsync(string route)
    {
        using var resp = await _http.GetAsync($"api/v1/{route}");
        return resp.StatusCode != System.Net.HttpStatusCode.NotFound;
    }

    // ---- Connections ----------------------------------------------------------------------------

    /// <summary>
    /// Creates an MsSql connection via the API and registers its deletion. The wizard's Step-1 test
    /// gate is bypassed here (seeding is a raw POST), so this works even when the UI create path is
    /// blocked. SqlAuth needs a Username + SecretKeyName, so a benign pair is supplied.
    /// </summary>
    public async Task<string> CreateConnectionAsync(
        string name,
        string connectionType = "MsSql",
        string server = "localhost",
        int port = 1433,
        string database = "SeedDb",
        bool encrypt = true,
        bool trustServerCertificate = true)
    {
        var body = new Dictionary<string, object?>
        {
            ["name"] = name,
            ["serviceType"] = connectionType,
            ["server"] = server,
            ["port"] = port,
            ["database"] = database,
            ["authenticationType"] = "SqlAuth",
            ["authentication"] = new Dictionary<string, string?>
            {
                ["Type"] = "SqlAuth",
                ["Username"] = "seed_user",
                ["SecretManagerName"] = "EnvSecrets",
                ["SecretKeyName"] = "OPS_PASSWORD",
            },
            ["encrypt"] = encrypt,
            ["trustServerCertificate"] = trustServerCertificate,
        };
        (await _http.PostAsJsonAsync("api/v1/connections", body)).EnsureSuccessStatusCode();
        _cleanups.Add(() => DeleteConnectionAsync(name));
        return name;
    }

    public async Task DeleteConnectionAsync(string name)
    {
        using var _ = await _http.DeleteAsync($"api/v1/connections/{Uri.EscapeDataString(name)}");
    }

    /// <summary>Register a connection created THROUGH THE UI for teardown.</summary>
    public void TrackConnectionForCleanup(string name) => _cleanups.Add(() => DeleteConnectionAsync(name));

    // ---- DataStores -----------------------------------------------------------------------------

    /// <summary>Creates a DataStore bound to an existing connection and registers its deletion.</summary>
    public async Task<string> CreateDataStoreAsync(
        string name,
        string connectionName,
        string storeType = "MsSql",
        string? description = null,
        string? displayName = null)
    {
        var body = new Dictionary<string, object?>
        {
            ["name"] = name,
            ["connectionName"] = connectionName,
            ["storeType"] = storeType,
            ["description"] = description,
            ["displayName"] = displayName,
            ["paths"] = Array.Empty<object>(),
        };
        (await _http.PostAsJsonAsync("api/v1/datastores", body)).EnsureSuccessStatusCode();
        _cleanups.Add(() => DeleteDataStoreAsync(name));
        return name;
    }

    public async Task DeleteDataStoreAsync(string name)
    {
        using var _ = await _http.DeleteAsync($"api/v1/datastores/{Uri.EscapeDataString(name)}");
    }

    /// <summary>Register a DataStore created THROUGH THE UI for teardown.</summary>
    public void TrackDataStoreForCleanup(string name) => _cleanups.Add(() => DeleteDataStoreAsync(name));

    // ---- Secret Managers ------------------------------------------------------------------------

    /// <summary>Creates a secret manager and registers its deletion.</summary>
    public async Task<string> CreateSecretManagerAsync(
        string name,
        string secretManagerType = "EnvironmentVariable",
        string? description = null)
    {
        var body = new Dictionary<string, object?>
        {
            ["name"] = name,
            ["secretManagerType"] = secretManagerType,
            ["description"] = description,
            ["configuration"] = new Dictionary<string, object?>(),
        };
        (await _http.PostAsJsonAsync("api/v1/secret-managers", body)).EnsureSuccessStatusCode();
        _cleanups.Add(() => DeleteSecretManagerAsync(name));
        return name;
    }

    public async Task DeleteSecretManagerAsync(string name)
    {
        using var _ = await _http.DeleteAsync($"api/v1/secret-managers/{Uri.EscapeDataString(name)}");
    }

    /// <summary>Register a secret manager created THROUGH THE UI for teardown.</summary>
    public void TrackSecretManagerForCleanup(string name) => _cleanups.Add(() => DeleteSecretManagerAsync(name));

    public async ValueTask DisposeAsync()
    {
        // Run cleanups in reverse creation order; never let one failure strand the rest.
        for (var i = _cleanups.Count - 1; i >= 0; i--)
        {
            try { await _cleanups[i](); } catch { /* best-effort teardown */ }
        }
        _http.Dispose();
    }
}
