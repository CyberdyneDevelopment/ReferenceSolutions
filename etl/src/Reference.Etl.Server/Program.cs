using Serilog.Extensions.Logging;
using Serilog.Enrichers.Span;
using System.Globalization;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using FastEndpoints.Swagger;
using Fdw.ServiceTypes;
using Reference_Etl_Server.Generated;
using Fdw.Hosting.Extensions;
using Fdw.SignalR;
using Fdw.Operations;
using Fdw.Services.Connections;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Projects;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Execution;
using Fdw.Services.Etl.Projects.Policy;
using Fdw.Services.Multitenancy;
using Fdw.Services.Authentication;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Authorization;
// Microsoft.AspNetCore.Authentication + InternalApiKey scheme removed; OpenIddict handles RS256 validation.
using Fdw.Services.Pipelines;
using Fdw.Abstractions;
using Fdw.Orchestration.Pipelines.Abstractions;
using Fdw.Results;
using Fdw.Services.Resiliency;
using Fdw.Services.Credentials;
using Fdw.Services.DataVault;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using ReferenceSecretManagers.EnvironmentVariable.Services;
using Fdw.Services.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Reference.Etl.Server.Logging;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using Serilog;
using ReferenceConnections.MsSql;

namespace Reference.Etl.Server;

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

            // Inject assembly version into Serilog configuration before host build
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
            var startupLogger = loggerFactory.CreateLogger("Reference.Etl.Server.Startup");

            ProgramLog.ServerStarting(startupLogger);

            // Why: configurationSchema.json is deserialized directly via STJ (not IConfiguration
            // binding) so the polymorphic ConnectionConfiguration/SecretManagerConfiguration
            // dispatch works. Fails fast at startup on missing or malformed file.
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

            // EtlPipelineConfigurationProvider is already registered by StreamingPipelineType and
            // BatchCopyPipelineType themselves (idempotent TryAddSingleton) — see service-domain-patterns skill.

            // Why: registers IOrchestrationNodeConfigurationProvider, orchestrator, and
            // OrchestrationNodeOrchestratorBackgroundService (FDW-388).
            OrchestrationTypes.ConfigureOrchestrationOptions(builder);
            OrchestrationTypes.RegisterOrchestrationServices(builder, loggerFactory, "ConfigurationDb");
            builder.Services.TryAddSingleton<IExecutionCompletionSignaler, ExecutionCompletionSignaler>();
            // Why: IProjectExecutionStatusReader depends on IExecutionTracker (Scoped), so it must
            // be Scoped itself — a Singleton cannot hold a Scoped dependency (captive dependency).
            builder.Services.TryAddScoped<IProjectExecutionStatusReader, ProjectExecutionStatusReader>();
            builder.Services.TryAddSingleton<IServerPolicyDefaults, ServerPolicyDefaults>();
            builder.Services.TryAddSingleton<IEffectivePolicyResolver, EffectivePolicyResolver>();





            // Why: ProjectExecutionQueue is a singleton channel bridge — registered separately
            // because it is a concrete class (not a ServiceTypeCollection type) that is consumed
            // directly by UnifiedTriggerEndpoint and ApproveExecutionEndpoint.
            // OrchestrationNodeOrchestratorBackgroundService is registered inside OrchestrationTypes.Register().
            builder.Services.AddSingleton<ProjectExecutionQueue>();


            builder.Services.AddAuthorization();
            builder.Services.AddRateLimiter(_ => { });

            builder.Services.AddFastEndpoints();
            builder.Services.SwaggerDocument(o =>
            {
                o.DocumentSettings = s =>
                {
                    s.Title = "Reference.Etl.Server";
                    s.Version = "v1";
                    s.Description = "Fdw ETL Server - Pipeline execution and job management.";
                };
            });

            var app = builder.Build();

            // --- Phase 2: Initialize (after Build, before Run) ---

            // Why: everything between here and the matching ClearSystemAuthenticationContext call
            // right before app.RunAsync() below reads ConfigurationDb via
            // IConfigurationGateway/DataGateway BEFORE any request is served or background execution
            // is dequeued — fail-closed security.fn_TenantFilter would deny every one of these reads
            // without an explicit elevation. Establish the ambient
            // IAuthenticationContextAccessor.Current as a SystemAuthenticationContext for this
            // synchronous bootstrap window ONLY, then clear it back to null before the app starts
            // accepting requests/dequeuing work — see the accessor clear below for why this cannot
            // leak into request/execution scope.
            var authContextAccessor = app.Services.GetRequiredService<IAuthenticationContextAccessor>();
            authContextAccessor.Current = new SystemAuthenticationContext();

            // Why: MultitenancyTypes.Configure (Phase 1, before Build) already registered the single
            // selected IMultitenancyType instance — resolve it here to derive the pipeline flag the old
            // hardcoded UseFrameworkApplicationPipeline(false) argument used to skip.
            var hasMultitenancy = app.Services.GetRequiredService<IMultitenancyType>().EnablesTenantResolution;

            PlatformServices.Initialize(app, loggerFactory);
            OrchestrationTypes.InitializeOrchestration(app.Services, loggerFactory);

            // Why: behind the preview proxy chain (Cloudflare/nginx HTTPS -> Caddy HTTP -> Kestrel),
            // X-Forwarded-Proto must be honored so Request.Scheme reports https. KnownNetworks/Proxies
            // cleared to trust the (preview-only) upstream. Must run before authentication.
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                KnownIPNetworks = { },
                KnownProxies = { },
            });

            app.UseFrameworkApplicationPipeline(hasMultitenancy);

            app.MapRealTimeHubs(loggerFactory);
            app.MapFrameworkHealthEndpoint("Reference.Etl.Server");
            // Why: UseFdwFastEndpoints registers PermissionClaimsPreProcessor globally so any
            // endpoint declaring Policies("resource:action") is enforced against the JWT perm claims.
            // ETL endpoints are anonymous/API-key today, so the pre-processor no-ops on them; this is
            // forward-cover for org-scoped permissions and keeps the pipeline symmetric with the API.
            app.UseFdwFastEndpoints(config =>
            {
                config.Endpoints.RoutePrefix = "api/v1";
            });
            app.UseSwaggerGen();
            app.MapScalarApiReference(options =>
            {
                options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
            });

            ProgramLog.ServerStarted(startupLogger);

            // Why: clears the bootstrap-only SystemAuthenticationContext established right after
            // Build() above — see that comment for the full rationale. By the time the first
            // request/background execution runs, Current is unconditionally null; the ETL
            // background services (PipelineExecutionBackgroundService /
            // OrchestrationNodeOrchestratorBackgroundService) then set their own scoped
            // WorkAuthenticationContext per execution, and RequestContextMiddleware sets its own
            // per HTTP request — neither ever inherits this bootstrap value.
            authContextAccessor.Current = null;

            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ETL Server terminated unexpectedly: {ex}");
            Log.Fatal(ex, "ETL Server terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
