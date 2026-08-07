using Serilog.Extensions.Logging;
using Serilog.Enrichers.Span;
using System.Globalization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FastEndpoints;
using FastEndpoints.Swagger;
using Fdw.ServiceTypes;
using Reference_Scheduler_Server.Generated;
using Fdw.Hosting.Extensions;
using Fdw.Services.Authentication;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Connections;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Pipelines.Clients;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Resiliency;
using Fdw.Services.Scheduling;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Credentials;
using Fdw.Services.DataVault;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using ReferenceSecretManagers.EnvironmentVariable.Services;
using Fdw.Services.Users;
using Fdw.Web.Http.Authentication;
using FdwSchedulerConfiguration = Fdw.Services.Scheduling.SchedulerConfiguration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reference.Scheduler.Server.Auth;
using Reference.Scheduler.Server.Configuration;
using Reference.Scheduler.Server.Logging;
using Reference.Scheduler.Server.Services;
using Reference.Scheduler.Server.Validation;
using Scalar.AspNetCore;
using Serilog;
using ReferenceConnections.MsSql;

namespace Reference.Scheduler.Server;

[ExcludeFromCodeCoverage]
public static class Program
{
#pragma warning disable MA0051 // Method is too long - sequential startup registration, not complex
    public static async Task<int> Main(string[] args)
#pragma warning restore MA0051
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration["Serilog:Properties:Version"] =
                System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion ?? "unknown";

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
            var startupLogger = loggerFactory.CreateLogger("Reference.Scheduler.Server.Startup");

            StartupLog.ServerStarting(startupLogger);

            // Why: configurationSchema.json is deserialized directly via STJ (not IConfiguration
            // binding) so the polymorphic ConnectionConfiguration/SecretManagerConfiguration
            // dispatch works. Fails fast at startup on missing or malformed file.
            // Why: cacheless role — scheduler must read fresh configuration on every call to avoid
            // cross-process staleness when the api server writes new schedule/connection rows.
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

            // Why: Lazy<IDataGateway> must be in DI before any domain provider is registered.
            // Providers resolve it on first configuration query — not at registration time.
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

            // --- Phase 1: Configure + Register (before Build) ---

            // ONE PlatformServices sweep replaces the per-domain Configure/Register ceremony:
            // every [ServiceTypeCollection] discovered by the generated module initializer
            // participates (see obj/generated PlatformServicesRegistration.g.cs). Multitenancy is a
            // "declared choice" domain (MultitenancyTypes) — its self-selecting Configure resolves the
            // single option named by ConfigurationSchema.Multitenancy (this host's configurationSchema.json
            // declares "SingleTenant"), so it participates in this same sweep with no separate manual block.
            PlatformServices.Configure(builder, loggerFactory);
            PlatformServices.Register(builder, loggerFactory);
            // SecretManagerConfigurationProvider is now registered by every secret-manager-kind
            // [ServiceTypeOption] itself (idempotent TryAddSingleton) — see service-domain-patterns skill.

            // ConnectionConfigurationProvider (and the IDataConnectionProvider/IServiceConnectionProvider
            // forwarding registrations, formerly ConnectionTypes.RegisterAdditionalInterfaces) are now
            // registered by every connection-kind [ServiceTypeOption] itself (idempotent TryAddSingleton)
            // — see service-domain-patterns skill.

            // Why: SchedulerConfigurationProvider hydrates IOptionsMonitor<List<SchedulerConfiguration>>
            // from sched.Scheduler via ConfigurationGateway. SchedulerConfiguration drives the background
            // service (DataStoreName / PathName / ScheduleContainerName); ScheduleConfigurationProvider
            // is separate and covers schedule definitions (the rows inside sched.Schedule). Both are now
            // registered by DefaultSchedulerType itself (RegisterRequiredServices / Configure phases,
            // idempotent TryAddSingleton) — see service-domain-patterns skill.

            builder.Services.AddAuthorization();
            builder.Services.AddRateLimiter(_ => { });

            // ========================================================================
            // Scheduler Server Services
            // ========================================================================

            // Why: App-specific IOptions<T> bindings from appsettings.json.
            builder.Services.Configure<Configuration.SchedulerConfiguration>(
                builder.Configuration.GetSection("Scheduler"));
            builder.Services.Configure<EtlDispatchConfiguration>(
                builder.Configuration.GetSection("EtlDispatch"));
            builder.Services.Configure<PreComputeOptions>(
                builder.Configuration.GetSection("PreCompute"));

            // Why: the scheduler's outbound identity for service-to-service dispatch. Only the names
            // needed to resolve the client secret are configured here — the secret value is read at
            // runtime via the secret manager (EnvSecrets → FDW_SECRET_SCHEDULER_CLIENT_SECRET), never
            // inlined. See OutboundCredentialAccessTokenProvider.
            builder.Services.Configure<OutboundClientCredentialsConfiguration>(
                builder.Configuration.GetSection(OutboundClientCredentialsConfiguration.SectionName));

            builder.Services.AddSingleton<IValidateOptions<ScheduleConfiguration>, ScheduleConfigurationValidator>();

            // Why: IFrameworkSchedulingService needs the runtime SchedulerConfiguration row from
            // sched.Scheduler. SchedulerConfigurationProvider reads through ConfigurationGateway,
            // so resolve it here and grab the first config. IOptionsMonitor only holds appsettings-
            // bound values, which is empty for this service in production.
            builder.Services.AddScoped<IFrameworkSchedulingService>(sp =>
            {
                var provider = sp.GetRequiredService<IServiceConfigurationProvider<FdwSchedulerConfiguration>>();
                var result = provider.Get().GetAwaiter().GetResult();
                if (!result.IsSuccess || result.Value is null || result.Value.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No SchedulerConfiguration rows in sched.Scheduler — cannot construct DefaultSchedulingService.");
                }
                return new DefaultSchedulingService(
                    sp.GetRequiredService<ILogger<DefaultSchedulingService>>(),
                    sp.GetRequiredService<IDataGateway>(),
                    result.Value[0],
                    sp.GetService<ITenantContext>());
            });

            // Why: Server-to-server HTTP client that dispatches pipeline jobs to the ETL server.
            // BearerTokenHandler attaches a client-credentials bearer token (scope 'fdw.api' — the
            // registered OpenIddict scope 'fdw.scheduler' is permitted to request) acquired by
            // OutboundCredentialAccessTokenProvider, so the ETL server authorizes the dispatch as the
            // 'fdw.scheduler' service identity via the token's 'perm' claim ('pipelines:execute',
            // baked from the ServicePipelineRunner role independent of scope). The handler is
            // transient; the token provider is scoped so the secret resolves per dispatch scope while
            // the outbound service caches the minted token.
            builder.Services.AddTransient<BearerTokenHandler>();
            builder.Services.AddScoped<IAccessTokenProvider, OutboundCredentialAccessTokenProvider>();

            builder.Services.AddHttpClient<PipelineJobHttpClient>((sp, client) =>
            {
                var dispatchConfig = sp.GetRequiredService<IOptions<EtlDispatchConfiguration>>();
                client.BaseAddress = new Uri(dispatchConfig.Value.BaseUrl.TrimEnd('/') + "/api/v1/");
                client.Timeout = TimeSpan.FromSeconds(dispatchConfig.Value.TimeoutSeconds);
            }).AddHttpMessageHandler<BearerTokenHandler>();
            builder.Services.AddScoped<IPipelineJobClient>(sp => sp.GetRequiredService<PipelineJobHttpClient>());
            builder.Services.AddScoped<IEtlDispatchService, EtlDispatchService>();

            // Why: Pre-compute services are application-specific background tasks that call
            // the API server to run calculations on a schedule.
            builder.Services.AddScoped<ICalculationUsageRepository, CalculationUsageRepository>();
            builder.Services.AddHttpClient<CalculationApiClient>((sp, client) =>
            {
                var preComputeConfig = sp.GetRequiredService<IOptions<PreComputeOptions>>();
                client.BaseAddress = new Uri(preComputeConfig.Value.ApiBaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            builder.Services.AddScoped<ICalculationApiClient>(sp => sp.GetRequiredService<CalculationApiClient>());

            builder.Services.AddHostedService<SchedulerBackgroundService>();
            builder.Services.AddHostedService<PreComputeCalculationsJob>();

            builder.Services.AddFastEndpoints();
            builder.Services.SwaggerDocument(o =>
            {
                o.DocumentSettings = s =>
                {
                    s.Title = "Reference.Scheduler.Server";
                    s.Version = "v1";
                    s.Description = "Fdw Scheduler Server - Schedule management and evaluation.";
                };
            });

            var app = builder.Build();

            // --- Phase 2: Initialize (after Build, before Run) ---

            // Why: everything between here and the matching ClearSystemAuthenticationContext call
            // right before app.RunAsync() below (three-phase Initialize, the scheduler-config read
            // just below) reads ConfigurationDb via IConfigurationGateway/DataGateway BEFORE any
            // request is served or schedule is dequeued — fail-closed security.fn_TenantFilter would
            // deny every one of these reads without an explicit elevation. Establish the ambient
            // IAuthenticationContextAccessor.Current as a SystemAuthenticationContext for this
            // synchronous bootstrap window ONLY, then clear it back to null before the app starts
            // accepting requests/running the schedule loop — see the accessor clear below for why
            // this cannot leak into request/execution scope.
            var authContextAccessor = app.Services.GetRequiredService<IAuthenticationContextAccessor>();
            authContextAccessor.Current = new SystemAuthenticationContext();

            // Why: MultitenancyTypes.Configure (Phase 1, before Build) already registered the single
            // selected IMultitenancyType instance — resolve it here to derive the pipeline flag the old
            // hardcoded UseFrameworkApplicationPipeline(false) argument used to skip.
            var hasMultitenancy = app.Services.GetRequiredService<IMultitenancyType>().EnablesTenantResolution;

            PlatformServices.Initialize(app, loggerFactory);

            // Why: Validate scheduler database configuration loaded. If no sched.Scheduler row
            // exists, fail loud per CLAUDE.md no-fallbacks rule — there is no acceptable default.
            // Read through SchedulerConfigurationProvider (ConfigurationGateway-backed); the
            // IOptionsMonitor would only carry appsettings-bound values, which is empty here.
            var schedulerProvider = app.Services.GetRequiredService<IServiceConfigurationProvider<FdwSchedulerConfiguration>>();
            var schedulerResult = await schedulerProvider.Get().ConfigureAwait(false);
            var schedulerConfig = (schedulerResult.IsSuccess && schedulerResult.Value is { Count: > 0 } list)
                ? list[0]
                : null;
            if (schedulerConfig is null)
            {
                StartupLog.SchedulerConfigurationMissing(startupLogger);
                return 1;
            }
            if (string.IsNullOrEmpty(schedulerConfig.DataStoreName) ||
                string.IsNullOrEmpty(schedulerConfig.PathName) ||
                string.IsNullOrEmpty(schedulerConfig.ScheduleContainerName))
            {
                StartupLog.SchedulerConfigurationIncomplete(
                    startupLogger,
                    schedulerConfig.DataStoreName ?? "(null)",
                    schedulerConfig.PathName ?? "(null)",
                    schedulerConfig.ScheduleContainerName ?? "(null)");
                return 1;
            }

            // Why: preview/staging sits behind a proxy chain (nginx -> Caddy) that terminates
            // TLS and forwards X-Forwarded-Proto: https over plain HTTP. ForwardedHeaders must run
            // before authentication so Request.Scheme is 'https' for redirect URIs, cookie Secure
            // flags, and IsHttps checks. KnownIPNetworks/KnownProxies cleared to trust the local proxy.
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                KnownIPNetworks = { },
                KnownProxies = { },
            });

            app.UseFrameworkApplicationPipeline(hasMultitenancy);

            app.MapFrameworkHealthEndpoint("Reference.Scheduler.Server");
            // Why: UseFdwFastEndpoints registers PermissionClaimsPreProcessor globally so any
            // endpoint declaring Policies("resource:action") is enforced against the JWT perm claims.
            // Scheduler endpoints are anonymous/API-key today, so the pre-processor no-ops on them; this
            // is forward-cover for org-scoped permissions and keeps the pipeline symmetric with the API.
            app.UseFdwFastEndpoints(config =>
            {
                config.Endpoints.RoutePrefix = "api/v1";
            });
            app.UseSwaggerGen();
            app.MapScalarApiReference(options =>
            {
                options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
            });

            StartupLog.ServerStarted(startupLogger);

            // Why: clears the bootstrap-only SystemAuthenticationContext established right after
            // Build() above — see that comment for the full rationale. By the time the first
            // request/schedule tick runs, Current is unconditionally null; the scheduler's own
            // background execution path sets its scoped WorkAuthenticationContext per run, and
            // RequestContextMiddleware sets its own per HTTP request — neither ever inherits this
            // bootstrap value.
            authContextAccessor.Current = null;

            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Scheduler Server terminated unexpectedly: {ex}");
            Log.Fatal(ex, "Scheduler Server terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
