using Serilog.Extensions.Logging;
using Serilog.Enrichers.Span;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastEndpoints;
using FastEndpoints.Swagger;
using Fdw.Configuration.Abstractions;
using Fdw.Hosting.Abstractions.Configuration;
using Fdw.Hosting.Configuration;
using Fdw.Hosting.Extensions;
using Fdw.Hosting.Models;
using Fdw.Services.Agents;
using Fdw.Services.Audit;
using Fdw.Services.Authentication;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Calculations;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Abstractions.Caching;
using Fdw.Services.Calculations.Caching;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.HealthChecks.Monitoring;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;

using Fdw.Services.Etl;
using Fdw.Services.Messaging;
using Fdw.Services.Multitenancy;
using Fdw.ServiceTypes;
using Reference_Api.Generated;
using Fdw.Services.Notifications;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Clients;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Fdw.Services.RateLimiting.Extensions;
using Fdw.Services.Resiliency;
using Fdw.Services.Scheduling;
using Fdw.Services.Scheduling.Clients;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using ReferenceSecretManagers.EnvironmentVariable.Services;
using Fdw.Services.SessionState;
using Fdw.Operations;
using Fdw.Operations.Endpoints;
using Fdw.Services.Settings;
using Fdw.Services.Credentials;
using Fdw.Services.DataVault;
using Fdw.Services.Users;
using Fdw.Services.Workflows;
using Fdw.SignalR;
using Fdw.UI.Themes;
using Fdw.UI.Themes.Configuration;
using Fdw.Web.Api.OpenApi;
using Reference.Api.OpenApi;
using Fdw.Web.Clients.Abstractions.Registration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using ReferenceAuthentication.TokenProviders;
using Reference.Api.Logging;
using Scalar.AspNetCore;
using Serilog;
using Fdw.Web.Http.Authentication;
using Fdw.Services.Etl.Projects.Providers;
using ReferenceConnections.MsSql;
using ReferenceConnections.MsSql.DataVault.Registration;
using ReferenceCredentials.Sql.Registration;

namespace Reference.Api;

/// <summary>
/// Application entry point with FDW hosting extensions, OpenTelemetry, and multi-tenant theming.
/// </summary>
[ExcludeFromCodeCoverage]
public static class Program
{
    // Why: Scalar PostProcess runs per-request, so the hosts list must be a static readonly
    // field rather than allocated each time (CA1861). Behind Caddy reverse proxy, Request.Scheme
    // reports http even with ForwardedHeaders middleware — we hardcode the public https origins
    // so Scalar's "Try It" doesn't get blocked as mixed content.
    private static readonly string[] ScalarPreviewHosts =
    {
        "https://api-1-2-0-preview.cyberdynedevelopment.dev",
        "https://localhost:5007",
        "http://localhost:5000",
    };

    public static async Task<int> Main(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration["Serilog:Properties:Version"] =
                System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion ?? "unknown";

            // ========================================================================
            // FDW Hosting Startup
            // ========================================================================
            // Serilog, inline rather than behind an extension method. Stage 1 is a console-only
            // startup logger so MessageLogging works before the host is built; stage 2 replaces it
            // with the full appsettings-driven configuration once the host exists.
            var startupSerilog = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithSpan()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                    formatProvider: CultureInfo.InvariantCulture);

            Log.Logger = startupSerilog.CreateLogger();

            var loggerFactory = LoggerFactory.Create(b => b
                .AddSerilog(Log.Logger)
                .SetMinimumLevel(LogLevel.Information));

            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithSpan()
                    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);
            });
            var startupLogger = loggerFactory.CreateLogger("Reference.Api.Startup");

            ProgramLog.ApplicationStarting(startupLogger);

            // ========================================================================
            // Bootstrap: Connection + Configuration + Secret Manager
            // ========================================================================
            // Connection:     ConfigurationDb is reached over MsSql (MsSqlConnectionFactory).
            // Configuration:  ConfigurationDb is the single configuration source. The minimal
            //                 bootstrap needed to reach it — the ConfigurationDb connection plus
            //                 the SecretManager declaration — ships in `configurationSchema.json`
            //                 (next to this app). Every other connection / datastore / dataset /
            //                 auth / pipeline / schedule / etc. is a row INSIDE ConfigurationDb.
            // Secret Manager: EnvironmentVariableSecretManager ("EnvSecrets") resolves secrets
            //                 from FDW_SECRET_* environment variables. The ConfigurationDb login
            //                 password comes from FDW_SECRET_CONFIG_PASSWORD; other databases use
            //                 FDW_SECRET_{AUTH,TENANT,ETL,SCHED,OPS,CONFIG_RO,NFL}_PASSWORD.
            //
            // Where each value lives:
            //   - configurationSchema.json          → bootstrap ConfigurationDb connection + the
            //                                         EnvSecrets secret-manager declaration
            //   - FDW_SECRET_* environment variables → ALL secret values (DB passwords, JWT signing
            //                                         key, InternalApi key). Supplied by the host
            //                                         (systemd `Environment=` on VM 104 / the
            //                                         per-slot preview env-file) — NOT appsettings.
            //   - appsettings(.{Environment}).json   → non-secret settings: Serilog, OpenTelemetry,
            //                                         ServiceEndpoints, InternalApi section, Cors,
            //                                         CalculationCache, Support.
            //
            // Why STJ: configurationSchema.json is deserialized via System.Text.Json (bypasses
            // IConfiguration binding) so polymorphic ConnectionConfiguration/SecretManagerConfiguration
            // dispatch to concrete subtypes works correctly.
            // Why: caching is built into DataGateway + ConfigurationGateway and is ON by default,
            // gated by the DataGateway:EnableCache config knob. Reference.Api is the CACHED role
            // (EnableCache=true in appsettings.json) — config reads are served from the shared
            // DataGatewayResultCache singleton and invalidated on write. ETL/Scheduler set
            // DataGateway:EnableCache=false (cacheless). No per-call cache wiring is needed here.
            builder.Services.AddConfigurationGateway<MsSqlConnectionFactory>(
                "configurationSchema.json",
                // Why a constructor call rather than a type argument: AddConfigurationGateway no longer
                // takes TSecretManager. The caller names the constructor, so the compiler checks that the
                // secret manager can actually be built — and the logical name the schema declares is
                // passed in rather than inferred.
                (sp, name) => new EnvironmentVariableSecretManager(
                    sp.GetRequiredService<ILogger<EnvironmentVariableSecretManager>>(),
                    sp.GetRequiredService<EnvironmentVariableConfiguration>(),
                    name));

            // ========================================================================
            // OpenTelemetry
            // ========================================================================
            // OpenTelemetry, inline rather than behind an extension method: nothing in the service
            // graph needs it to run — it is this host's observability wiring, read from config.
            var otelServiceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "Reference.Api";
            var tracingEnabled = builder.Configuration.GetValue("OpenTelemetry:Tracing:Enabled", true);
            var metricsEnabled = builder.Configuration.GetValue("OpenTelemetry:Metrics:Enabled", true);
            var exportToConsole = builder.Configuration.GetValue("OpenTelemetry:Tracing:ExportToConsole", false);

            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(otelServiceName)
                    .AddAttributes([
                        new("deployment.environment", builder.Environment.EnvironmentName),
                        new("host.name", Environment.MachineName)
                    ]))
                .WithTracing(tracing =>
                {
                    if (!tracingEnabled) return;
                    tracing.AddAspNetCoreInstrumentation(options => options.RecordException = true);
                    if (exportToConsole) tracing.AddConsoleExporter();
                })
                .WithMetrics(metrics =>
                {
                    if (!metricsEnabled) return;
                    metrics.AddAspNetCoreInstrumentation();
                    if (exportToConsole) metrics.AddConsoleExporter();
                });

            // ========================================================================
            // FDW Service Types — Phase 1 (Configure + Register, before Build)
            // ========================================================================
            // Why: Lazy<IDataGateway> must be in DI before any domain provider is registered.
            // OpenIddict stores, connection factories, and config providers all take Lazy<IDataGateway>
            // in their constructors so they can defer gateway resolution to first use, avoiding
            // circular startup dependencies.
            builder.Services.AddSingleton(sp =>
            {
                // Why: IDataGateway is Scoped; these singleton consumers deliberately share ONE
                // app-lifetime gateway resolved at first use. The dedicated scope (never disposed,
                // kept alive by this closure) makes that an explicit choice — resolving from the
                // root provider is illegal under Development scope validation and was a silent
                // captive dependency in Production.
                var gatewayScope = sp.CreateScope();
                return new Lazy<IDataGateway>(() => gatewayScope.ServiceProvider.GetRequiredService<IDataGateway>());
            });

            // ONE PlatformServices sweep replaces the per-domain Configure/Register ceremony:
            // every [ServiceTypeCollection] discovered by the generated module initializer
            // participates (see obj/generated PlatformServicesRegistration.g.cs). Multitenancy is a
            // "declared choice" domain (MultitenancyTypes) — its self-selecting Configure resolves the
            // single option named by ConfigurationSchema.Multitenancy (configurationSchema.json) and
            // drives that ONE option's Configure/RegisterRequiredServices, so it participates in this
            // same sweep without a separate manual block.
            PlatformServices.Configure(builder, loggerFactory);
            PlatformServices.Register(builder, loggerFactory);

            // Why: DataflowGraphConfigurationProvider is an endpoint-only config provider living in
            // Fdw.Operations.Endpoints (which the Operations domain assembly can't reference),
            // and has no service consumer — so it stays an entry-point root here (same category as the
            // Notification header provider) until/unless an Operations.Endpoints scaffold is stood up.
            DataflowGraphConfigurationProvider.RegisterDomainConfiguration(builder.Services);

            // Why only the CONFIGURATION provider and not OrchestrationTypes: the Projects and Nodes
            // endpoints here are CRUD over orchestration-node configuration in ConfigurationDb, which
            // any host can do safely. Orchestration EXECUTION is not registered in this host on
            // purpose — OrchestrationNodeExecutionQueue is an in-memory Channel, so a host that
            // registers it drains its own queue and which process runs a trigger depends on which one
            // received the request. Execution lives in Reference.Etl.Server; the /etl/* endpoints here
            // proxy to it (see EtlOrchestrationProxyEndpoints). This registrar adds the provider trio
            // only — no queue, no orchestrator, no hosted service.
            OrchestrationNodeConfigurationProvider.RegisterDomainConfiguration(builder.Services);

            // Why: IConfigurationConnectionNameProvider is consumed by catalog endpoints that
            // need the configuration connection name at runtime for DataGateway routing.
            builder.Services.AddSingleton<IConfigurationConnectionNameProvider, DefaultConfigurationConnectionNameProvider>();

            // Why: ThemeConfigurationProvider lives in Fdw.UI.Themes which has no
            // ServiceTypeCollection. Registered here because FDW.Hosting doesn't reference UI.Themes.
            builder.Services.AddSingleton<ThemeConfigurationProvider>();

            // ========================================================================
            // Application Services
            // ========================================================================
            builder.Services.Configure<SupportOptions>(
                builder.Configuration.GetSection("Support"));

            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddFrameworkRateLimiting(loggerFactory);

            // Why: Proxy endpoints forward requests to ETL and Scheduler on behalf of the calling
            // user. They DELEGATE the user's own bearer token downstream (ForwardedUserTokenProvider)
            // so ETL/Scheduler validate the same token and enforce the user's permissions — a
            // client-credentials service token carries no perm claims and would be rejected (403)
            // once the downstreams enforce permissions. InternalApiKeyDelegatingHandler is deleted.
            builder.Services.Configure<ServiceEndpointsOptions>(
                builder.Configuration.GetSection(ServiceEndpointsOptions.SectionName));

            // Why scoped: reads the current request's Authorization header per request.
            builder.Services.AddScoped<IAccessTokenProvider, ForwardedUserTokenProvider>();

            // CORS, inline rather than behind an extension method: this is host wiring, not a
            // service any option owns — the policy comes straight from the "Cors" config section.
            var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()
                ?? new CorsOptions();
            if (corsOptions.Enabled)
            {
                builder.Services.AddCors(options =>
                {
                    options.AddDefaultPolicy(policy =>
                    {
                        if (corsOptions.Origins.Count > 0)
                        {
                            policy.WithOrigins(corsOptions.Origins.ToArray());
                        }
                        else
                        {
                            policy.SetIsOriginAllowed(origin =>
                                string.Equals(new Uri(origin).Host, "localhost", StringComparison.OrdinalIgnoreCase));
                        }

                        policy.WithMethods(corsOptions.Methods.ToArray())
                              .WithHeaders(corsOptions.Headers.ToArray())
                              .WithExposedHeaders(corsOptions.ExposedHeaders.ToArray())
                              .SetPreflightMaxAge(TimeSpan.FromSeconds(corsOptions.PreflightMaxAgeSeconds));

                        if (corsOptions.AllowCredentials)
                        {
                            policy.AllowCredentials();
                        }
                    });
                });

                // Why registered: the middleware pipeline reads it back later.
                builder.Services.AddSingleton(corsOptions);
            }

            // Why: TokenSwitchEndpoint proxies /auth/token-switch → /connect/token on this server
            // so the UI can use a stable route that doesn't expose the raw OpenIddict path.
            // BaseAddress is the server's own loopback address resolved from configuration.
            builder.Services.AddHttpClient("TokenSwitch", (sp, client) =>
            {
                var urls = builder.Configuration["ASPNETCORE_URLS"]
                    ?? builder.Configuration["Kestrel:Endpoints:Http:Url"]
                    ?? "http://localhost:5020";
                var baseUrl = urls.Split(';')[0].TrimEnd('/') + "/";
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // ========================================================================
            // FastEndpoints + Swagger
            // ========================================================================
            builder.Services.AddFastEndpoints();
            // Why: IHttpContextAccessor is needed by PermissionFilterDocumentProcessor to read
            // the current user's claims and filter the OpenAPI document by permissions.
            builder.Services.AddHttpContextAccessor();
            var dataSetQueryDocProcessor = new DataSetQueryDocumentProcessor();
            var permissionFilterProcessor = new PermissionFilterDocumentProcessor();
            builder.Services.SwaggerDocument(o =>
            {
                o.DocumentSettings = s =>
                {
                    s.Title = "Reference.Api";
                    s.Version = "v1";
                    s.Description = "Fdw Reference API demonstrating DataGateway, " +
                                    "SecretManager, and ConnectionProvider.";
                    s.DocumentProcessors.Add(dataSetQueryDocProcessor);
                    // Why: resolves [ValuesFrom] attributes on config DTOs to enum constraints
                    // so Scalar renders dropdowns for TypeCollection-backed properties.
                    s.DocumentProcessors.Add(new ValuesFromSchemaDocumentProcessor());
                    // Why: filters the OpenAPI document based on the current user's permissions
                    // so Scalar only shows endpoints the user can actually call.
                    s.DocumentProcessors.Add(permissionFilterProcessor);
                    // Why: normalize tags (collapse Auth/Authentication, dedupe, tag untagged ops)
                    // and give OpenIddict's /connect/token a tag + urlencoded form body (param names
                    // only) so Scalar renders a usable login form. Runs after the permission filter
                    // so it only touches operations the current user can see.
                    s.DocumentProcessors.Add(new AuthAndTagDocumentProcessor());
                    // Why: Behind Caddy reverse proxy, Request.Scheme reports http even with
                    // ForwardedHeaders middleware. Hardcode the public scheme so Scalar's
                    // "Try It" hits https and the browser doesn't reject as mixed content.
                    s.PostProcess = doc =>
                    {
                        doc.Servers.Clear();
                        foreach (var h in ScalarPreviewHosts)
                            doc.Servers.Add(new NSwag.OpenApiServer { Url = h });
                    };
                };
            });

            var app = builder.Build();

            // Why: everything between here and the matching ClearSystemAuthenticationContext call
            // right before app.RunAsync() below (three-phase Initialize, theme provider read, etc.)
            // reads ConfigurationDb via IConfigurationGateway/DataGateway BEFORE any request is
            // served — there is no HTTP-authenticated identity or per-run tenant context yet, so
            // fail-closed security.fn_TenantFilter would deny every one of these reads and the app
            // could not boot. Establish the ambient IAuthenticationContextAccessor.Current as an
            // explicit SystemAuthenticationContext for this synchronous bootstrap window ONLY, then
            // clear it back to null before the app starts accepting requests — see the accessor
            // clear below for why this cannot leak into request-handling code.
            var authContextAccessor = app.Services.GetRequiredService<IAuthenticationContextAccessor>();
            authContextAccessor.Current = new SystemAuthenticationContext();

            // Why: MultitenancyTypes.Configure (Phase 1, before Build) already registered the single
            // selected IMultitenancyType instance — resolve it here to derive the pipeline flag the old
            // manual Multitenancy block used to compute inline.
            var hasMultitenancy = app.Services.GetRequiredService<IMultitenancyType>().EnablesTenantResolution;

            // PlatformServices.Initialize runs every swept domain's Initialize in Group order
            // (SecretManager→Connection→DataGateway→DataVault→CredentialService→…→DataStore→DataSet→rest);
            // the Group DAG encodes the dependency order, so no hand-driven prerequisite pre-calls are needed.
            PlatformServices.Initialize(app, loggerFactory);

            dataSetQueryDocProcessor.Initialize(app.Services);
            permissionFilterProcessor.Initialize(app.Services);

            // Why: When behind a TLS-terminating reverse proxy (Caddy → localhost:port), trust
            // X-Forwarded-Proto/For/Host so Request.Scheme reports "https" and OpenAPI emits
            // https:// server URLs (otherwise Scalar "Try It" hits http:// and the browser
            // blocks the request as mixed content).
            // Why: KnownNetworks/KnownProxies cleared so the middleware accepts X-Forwarded-*
            // from any upstream (Caddy in this preview env). For prod, lock to specific CIDRs.
            var fhOptions = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                                 | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
                                 | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost,
            };
            fhOptions.KnownIPNetworks.Clear();
            fhOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(fhOptions);

            // API-62: write {errorCode, messages[]} envelopes on otherwise-bodyless 401/403
            // responses from the auth pipeline. Must be registered BEFORE the auth/endpoints
            // middleware so it can wrap their response writes.
            app.UseStatusCodePages(async statusContext =>
            {
                var status = statusContext.HttpContext.Response.StatusCode;
                if (status is not (401 or 403)) return;
                if (statusContext.HttpContext.Response.HasStarted) return;
                statusContext.HttpContext.Response.ContentType = "application/json";
                var code = status == 401 ? "Unauthorized" : "Forbidden";
                var msg = status == 401
                    ? "Authentication is required to access this resource."
                    : "You do not have permission to access this resource.";
                await statusContext.HttpContext.Response.WriteAsJsonAsync(new
                {
                    errorCode = code,
                    messages = new[] { msg }
                }).ConfigureAwait(false);
            });
            app.UseFrameworkApplicationPipeline(hasMultitenancy);
            // API-120: translate empty-body POST/PUT/PATCH to 400 instead of FastEndpoints' 415.
            // Why the options are declared here: which routes may arrive without a body is a property of
            // THIS host's surface. The middleware supplies the rule; the host supplies its own routes.
            app.UseMiddleware<ReferenceHosting.Middleware.EmptyBodyBadRequestMiddleware>(
                new ReferenceHosting.Middleware.EmptyBodyOptions
                {
                    BodylessPaths = ["/api/v1/auth/logout"],
                    BodylessRoutes =
                    [
                        new ReferenceHosting.Middleware.BodylessRoute("/api/v1/pipelines/", "/execute"),
                        new ReferenceHosting.Middleware.BodylessRoute("/api/v1/promotion/requests/", "/approve"),
                        new ReferenceHosting.Middleware.BodylessRoute("/api/v1/access-requests/", "/approve"),
                    ],
                });
            app.UseFastEndpoints(config =>
            {
                config.Endpoints.RoutePrefix = "api/v1";
                config.Security.RoleClaimType = "roles";
                // Why: API-62 — uniform error envelope. FastEndpoints' default validation-failure
                // body is {statusCode, message, errors:{field:[...]}}; flatten it to
                // {errorCode, messages[]} so every error response shares the same shape.
                config.Errors.ResponseBuilder = (failures, ctx, statusCode) =>
                {
                    var messages = failures
                        .Select(f => string.IsNullOrEmpty(f.PropertyName)
                            ? f.ErrorMessage
                            : $"{f.PropertyName}: {f.ErrorMessage}")
                        .ToArray();
                    return new
                    {
                        errorCode = "ValidationFailed",
                        messages
                    };
                };
            });
            app.UseSwaggerGen();
            app.MapRealTimeHubs(loggerFactory);
            // Why: Scalar is themed from cfg.Theme so both the Management UI and API docs share the same
            // brand palette. Theming JS also supports per-tenant themes via ?tenant= query parameter.
            var themeProvider = app.Services.GetRequiredService<Fdw.UI.Themes.ThemeConfigurationProvider>();
            var themesResult = themeProvider.Get().GetAwaiter().GetResult();
            // Why: themes are cosmetic (Scalar/UI brand palette). Reading .Value on a failed result
            // throws ("Cannot access value of a failed result"); guard on IsSuccess so a theme-load
            // failure degrades to the default palette instead of crashing startup. Required-config
            // failures (e.g. ConfigurationDb unreachable) fail-fast at the config bootstrap, not here.
            var themes = themesResult.IsSuccess ? themesResult.Value ?? [] : [];
            var defaultTheme = themes.FirstOrDefault(t => t.IsDefault)
                ?? themes.FirstOrDefault(t => string.Equals(t.Name, "fractal", StringComparison.OrdinalIgnoreCase))
                ?? (themes.Count > 0 ? themes[0] : null);

            var tenantsJs = BuildTenantsJs(themes);
            var defaultCss = defaultTheme != null ? BuildScalarCss(defaultTheme) : string.Empty;
            var showDemoCredentials = app.Environment.IsDevelopment();

            app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();

            app.MapScalarApiReference(options =>
            {
                options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
                options.WithTheme(ScalarTheme.None);
                options.AddHeadContent($@"
                    <style>
                        :root {{
                            {defaultCss}
                            --scalar-font-code: '{EscapeJs(defaultTheme?.FontFamilyMono ?? "JetBrains Mono, monospace")}';
                            --scalar-border-radius: {(defaultTheme?.BorderRadius ?? 6).ToString(CultureInfo.InvariantCulture)}px;
                        }}
                        {(showDemoCredentials ? @"
                        .demo-credentials {
                            position: fixed; top: 0; left: 0; right: 0; z-index: 9999;
                            background: var(--scalar-background-2);
                            border-bottom: 2px solid var(--scalar-color-red);
                            padding: 8px 20px; font-family: var(--scalar-font); font-size: 13px;
                            color: var(--scalar-color-1); display: flex; align-items: center;
                            gap: 20px; flex-wrap: wrap;
                        }
                        .demo-credentials .warning {{
                            background: var(--scalar-color-red); color: white; padding: 2px 8px;
                            border-radius: 3px; font-weight: 600; font-size: 11px; text-transform: uppercase;
                        }}
                        .demo-credentials code {{
                            background: rgba(0,0,0,0.3); padding: 2px 6px; border-radius: 3px;
                            font-family: var(--scalar-font-code); color: var(--scalar-color-orange);
                        }}
                        body {{ padding-top: 50px !important; }}" : string.Empty)}
                        .sidebar {{ border-right: 1px solid var(--scalar-border-color); }}
                    </style>
                    <script>
                        const themes = {tenantsJs};
                        (function applyTheme() {{
                            const urlParams = new URLSearchParams(window.location.search);
                            const hostname = window.location.hostname.split('.')[0];
                            const tenantKey = urlParams.get('tenant') || (themes[hostname] ? hostname : '{EscapeJs(defaultTheme?.Name ?? "default")}');
                            const theme = themes[tenantKey] || themes['{EscapeJs(defaultTheme?.Name ?? "default")}'];
                            if (!theme) return;
                            document.title = theme.title || 'API Reference';
                            const root = document.documentElement;
                            if (theme.colors) {{
                                for (const [key, value] of Object.entries(theme.colors)) {{
                                    root.style.setProperty(key, value);
                                }}
                            }}
                            if (theme.logoUrl) root.style.setProperty('--scalar-custom-logo', `url('${{theme.logoUrl}}')`);
                            if (theme.fontUrl) {{
                                const link = document.createElement('link');
                                link.rel = 'stylesheet'; link.href = theme.fontUrl;
                                document.head.appendChild(link);
                            }}
                            if (theme.font) root.style.setProperty('--scalar-font', theme.font);
                        }})();
                        {(showDemoCredentials ? @"
                        document.addEventListener('DOMContentLoaded', function() {
                            var banner = document.createElement('div');
                            banner.className = 'demo-credentials';
                            banner.innerHTML = '<span class=""warning"">Demo Only - Do Not Use in Production</span>' +
                                '<span><strong>User:</strong> <code>testuser</code> / <code>TestPassword1#</code></span>' +
                                '<span><strong>Admin:</strong> <code>admin</code> / <code>AdminPassword1#</code></span>';
                            document.body.insertBefore(banner, document.body.firstChild);
                        });" : string.Empty)}
                    </script>");
            })
            // Why: Scalar page itself is anonymous so the login endpoint is reachable without
            // first being authenticated. PermissionFilterDocumentProcessor filters the OpenAPI
            // document based on the current user — anonymous callers see only health + auth
            // endpoints, authenticated callers see their permitted operations, admins see all.
            .AllowAnonymous();
            app.MapFrameworkHealthEndpoint("Reference.Api");

            ProgramLog.ApplicationStarted(startupLogger);
            ProgramLog.ScalarAvailable(startupLogger);
            ProgramLog.SeqAvailable(startupLogger, "http://localhost:5341");

            // Why: clears the bootstrap-only SystemAuthenticationContext established right after
            // Build() above. This is the safety net that guarantees system elevation cannot leak
            // into request handling regardless of AsyncLocal/ExecutionContext flow semantics across
            // the Kestrel request-scheduling boundary — by the time the first request is served,
            // Current is unconditionally null. RequestContextMiddleware (Fdw.Hosting) then sets
            // Current explicitly per authenticated request; anonymous requests leave it null
            // (fail-closed), never falling back to this system value.
            authContextAccessor.Current = null;

            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            // Why: FlushFrameworkSerilog bounds the wait to DefaultFlushTimeout (5s) so an
            // unreachable Loki/Seq sink (GrafanaLoki is pointed at the staging VM) does not
            // delay the fatal error message or hang the process indefinitely (FDW-424).
            await Fdw.Hosting.Extensions.SerilogExtensions.FlushFrameworkSerilog();
        }
    }

    private static string BuildScalarCss(ThemeManagedConfiguration theme)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-color-accent: {theme.PrimaryColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-button-1: {theme.PrimaryColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-button-1-color: {theme.TextPrimary};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-background-1: {theme.BackgroundColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-background-2: {theme.SurfaceColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-background-3: {theme.SurfaceColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-color-1: {theme.TextPrimary};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-color-2: {theme.TextSecondary};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-border-color: {theme.TextSecondary}33;");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-color-green: {theme.SuccessColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-color-red: {theme.ErrorColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-color-orange: {theme.WarningColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-color-blue: {theme.InfoColor};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--scalar-font: '{EscapeJs(theme.FontFamily)}';");
        return sb.ToString();
    }

    private static string BuildTenantsJs(IReadOnlyList<ThemeManagedConfiguration> themes)
    {
        if (themes.Count == 0) return "{}";
        var sb = new StringBuilder();
        sb.Append('{');
        for (int i = 0; i < themes.Count; i++)
        {
            var t = themes[i];
            if (i > 0) sb.Append(',');
            sb.Append(CultureInfo.InvariantCulture, $@"
                ""{EscapeJs(t.Name)}"": {{
                    ""title"": ""{EscapeJs(t.DisplayName ?? t.AppName ?? t.Name)} API"",
                    ""logoUrl"": {(t.LogoUrl != null ? $@"""{EscapeJs(t.LogoUrl)}""" : "null")},
                    ""fontUrl"": null,
                    ""font"": ""{EscapeJs(t.FontFamily)}"",
                    ""colors"": {{
                        ""--scalar-color-accent"": ""{EscapeJs(t.PrimaryColor)}"",
                        ""--scalar-button-1"": ""{EscapeJs(t.PrimaryColor)}"",
                        ""--scalar-button-1-color"": ""{EscapeJs(t.TextPrimary)}"",
                        ""--scalar-background-1"": ""{EscapeJs(t.BackgroundColor)}"",
                        ""--scalar-background-2"": ""{EscapeJs(t.SurfaceColor)}"",
                        ""--scalar-background-3"": ""{EscapeJs(t.SurfaceColor)}"",
                        ""--scalar-color-1"": ""{EscapeJs(t.TextPrimary)}"",
                        ""--scalar-color-2"": ""{EscapeJs(t.TextSecondary)}"",
                        ""--scalar-border-color"": ""{EscapeJs(t.TextSecondary)}33"",
                        ""--scalar-color-green"": ""{EscapeJs(t.SuccessColor)}"",
                        ""--scalar-color-red"": ""{EscapeJs(t.ErrorColor)}"",
                        ""--scalar-color-orange"": ""{EscapeJs(t.WarningColor)}"",
                        ""--scalar-color-blue"": ""{EscapeJs(t.InfoColor)}""
                    }}
                }}");
        }
        sb.Append('}');
        return sb.ToString();
    }

    private static string EscapeJs(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
             .Replace("'", "\\'", StringComparison.Ordinal)
             .Replace("\"", "\\\"", StringComparison.Ordinal);
}
