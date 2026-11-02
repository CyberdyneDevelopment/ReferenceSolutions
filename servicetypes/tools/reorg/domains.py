import re
# ordered: first match wins. Applied to the project name with a trailing ".Tests" removed.
# A domain is named after the service domain the code already calls itself (Fdw.Services.<X>),
# or, for the shared foundation, after the concern it owns.
FDW_RULES = [
 # foundation, one named concern each
 (r"^Fdw\.Results(\.Abstractions)?$", "results"),
 (r"^Fdw\.(Collections(\..*)?|StateCollections(\..*)?)$", "collections"),
 (r"^Fdw\.(Messages|MessageLogging(\..*)?)$", "messagelogging"),
 (r"^Fdw\.(Registration\.SourceGenerators|Services\.Registration(\..*)?)$", "registration"),
 (r"^Fdw\.(Analyzers|CodeFixes|Conventions\..*|ServiceTypes\.(Analyzers|CodeFixes)|SourceGenerators)$", "analyzers"),
 (r"^Fdw\.Validation(\..*)?$", "validation"),
 (r"^Fdw\.(Security\..*|Claims\.Abstractions)$", "security"),
 (r"^Fdw\.(Abstractions|Types\.Abstractions|Expressions)$", "abstractions"),
 (r"^Fdw\.(Services|Services\.Abstractions|Processors\.Abstractions)$", "services"),
 (r"^Fdw\.Commands(\..*)?$", "commands"),
 # identity and access
 (r"^Fdw\.(Services\.(Authentication|ExternalIdentityProviders|Identity|SessionState)|Web\.Http\.Authentication).*", "authentication"),
 (r"^Fdw\.Services\.Authorization.*", "authorization"),
 (r"^Fdw\.Services\.Multitenancy.*", "multitenancy"),
 (r"^Fdw\.Services\.Users.*", "users"),
 # secrets
 (r"^Fdw\.Services\.SecretManagers.*", "secretmanagers"),
 (r"^Fdw\.Services\.(Credentials|DataVault|TokenManagers).*", "credentials"),
 # connections and the dialects they speak
 (r"^Fdw\.(Services\.Connections(\..*)?|Data\.(MsSql|PostgreSql|Sqlite|Http|OData|OpenApi)|Types\.MsSql|Sql\.Commands(\..*)?|Schema\.Ddl\.MsSql)$", "connections"),
 (r"^Fdw\.(Tds\.Endpoints|Protocols\.Tds|Services\.Workspaces.*)$", "workspaces"),
 (r"^Fdw\.(Services\.Catalog.*|Data\.Lineage)$", "catalog"),
 (r"^Fdw\.Configuration.*|^Fdw\.Services\.Settings.*", "configuration"),
 # data
 (r"^Fdw\.Data\.RowSources.*", "rowsources"),
 (r"^Fdw\.(Data\.Transformations.*|Services\.Transformations.*)", "transformations"),
 (r"^Fdw\.Data\.(DataSets|DataStores).*", "datasets"),
 (r"^Fdw\.(Schema(\..*)?|Data\.JsonSchema)$", "schema"),
 (r"^Fdw\.(Data(\..*)?|Services\.Data(\..*)?|Services\.Execution.*)$", "data"),
 # work
 (r"^Fdw\.(Services\.(Pipelines|Etl|EtlMappers)|Etl\.Abstractions|Orchestration\..*|Operations.*|UI\.Pipelines.*).*", "pipelines"),
 (r"^Fdw\.Services\.Scheduling.*", "scheduling"),
 (r"^Fdw\.(Calculations.*|Services\.Calculations.*|Web\.Calculations.*)", "calculations"),
 (r"^Fdw\.Services\.(Notifications|Messaging).*", "notifications"),
 # running it
 (r"^Fdw\.(Services\.Telemetry|Services\.Logging|Web\.Analytics).*", "telemetry"),
 (r"^Fdw\.Services\.(Resiliency|RateLimiting).*", "resiliency"),
 (r"^Fdw\.Services\.Quality.*", "quality"),
 (r"^Fdw\.Documents(\..*)?$", "documents"),
 (r"^Fdw\.(Services\.Hosts(\..*)?|Hosting\..*|SignalR)$", "hosts"),
 (r"^Fdw\.Web\.(Api|Endpoints|RestEndpoints|Http\..*|Clients(\..*)?|Search\..*)$", "endpoints"),
 # ai and tooling
 (r"^Fdw\.(Services\.Agents.*|Agents.*|Aui.*|Intelligence.*)", "agents"),
 (r"^Fdw\.Aegis.*", "aegis"),
 (r"^Fdw\.(Roslyn\..*|CodeBuilder\..*|Workspace\..*|Sql\.Workspace)$", "roslyn"),
 (r"^Fdw\.(Mcp\..*|WebMcp\..*)$", "mcp"),
 (r"^Fdw\.DevSession.*", "devsession"),
 (r"^Fdw\.(TUI\..*|VsCodeShell.*|Services\.Terminal.*)$", "shells"),
 (r"^Fdw\.UI(\..*)?$", "ui"),
]
RST = {
 "Connections":"connections","SecretManagers":"secretmanagers","Credentials":"credentials",
 "Authentication":"authentication","Auth":"authentication","SessionState":"authentication","Multitenancy":"multitenancy",
 "Data":"data","DataPaths":"data","Schedulers":"scheduling","Schedules":"scheduling",
 "Notifications":"notifications","Messaging":"notifications","Alerts":"notifications","Inbox":"notifications","UiFeedback":"notifications","TypeCollections":"pipelines",
 "Telemetry":"telemetry","Logging":"telemetry","Quality":"quality",
 "Hosts":"hosts","Host":"hosts","Endpoints":"endpoints","Proxy":"endpoints",
 "Operations":"pipelines","Etl":"pipelines","Documents":"documents","Configuration":"configuration","Calculations":"calculations",
 "Dataverses":"workspaces","Workspaces":"workspaces","Catalog":"catalog","AgentActions":"agents","Aegis":"aegis",
 "Nfl":"samples","Samples":"samples",
}
def domain_of(repo, name):
    n = re.sub(r"\.Tests$", "", name)
    if repo == "rst":
        m = re.match(r"^FdwReference([A-Za-z]+)", n)
        return RST.get(m.group(1)) if m else None
    for rx, d in FDW_RULES:
        if re.match(rx, n): return d
    return None

# Related small domains share a folder. Only the folder is shared: every project stays its own project,
# and each domain keeps its own expert. A domain not listed here sits directly under src/ and tests/.
GROUP = {}
for _g, _ds in {
    "foundation":    ["results", "collections", "messagelogging", "analyzers", "registration", "validation", "security", "abstractions", "services"],
    "identity":      ["authentication", "authorization", "multitenancy", "users"],
    "secrets":       ["secretmanagers", "credentials"],
    "observability": ["telemetry", "resiliency", "quality"],
    "ai":            ["agents", "aegis"],
    "tooling":       ["roslyn", "mcp", "devsession", "shells"],
}.items():
    for _d in _ds: GROUP[_d] = _g
