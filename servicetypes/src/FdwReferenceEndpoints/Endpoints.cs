using Fdw.Services.Connections.Endpoints.OpenApi;
using System;
using Fdw.Services.Hosts.Configuration;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.RestEndpoints.Security;
using Fdw.Web.RestEndpoints.OpenApi;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Fdw.Results;
using FastEndpoints;
using FastEndpoints.Swagger;
using System.Linq;

using Fdw.Services.RateLimiting.Extensions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy;
using FdwReferenceHost.Middleware;
using FdwReferenceHost.Models;
using Fdw.Services.Hosts;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog;
using FdwReferenceMultitenancy.Middleware;
using FdwReferenceEndpoints.Configuration;
using Fdw.Services.Hosts.Abstractions;
using Fdw.SignalR;
namespace FdwReferenceEndpoints;

/// <summary>
/// The parent of every endpoints collection.
/// </summary>
/// <remarks>
/// The level above a resource. ScheduleEndpoints holds the endpoints over schedules; this holds ScheduleEndpoints and its siblings, so there is a name for
/// "every one of these the application serves" that is not a list somebody maintains.
///
/// Its ServiceCategory is what puts it in PlatformServices and names the generated accessor, so
/// <c>PlatformServices.Endpoints</c> reaches all of them without going through any one domain's service
/// type. That is the level that was missing: a tag every the endpoints over one resource shares belongs on that resource's
/// collection, and something they all share belongs here. Before this, both had to be repeated per
/// member or hoisted into a host that owns neither.
///
/// TBase is the interface rather than a class because a non-generic base cannot be inserted:
/// each resource collection already derives a closed TypeCollectionBase, and that base slot is taken.
/// The interface is the only type they all are.
///
/// A group joins EndpointGroups rather than naming this collection as a parent. The parent/child
/// arguments on the collection attributes emit a partial class onto the parent, and a partial cannot
/// span assemblies - the parent is here and the groups are in another package, so that route silently
/// produces nothing. Membership has no such limit.
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(IEndpointTypeCollection),
    typeof(IEndpointTypeCollection),
    typeof(Endpoints),
    ServiceCategory = "Endpoints")]
public partial class Endpoints : ServiceTypeCollectionBase<IEndpointTypeCollection, IEndpointTypeCollection>
{
    // The implementation this option supplies to its domain -- the Implementation its domain row names.
    private const string HostImplementation = "Host";

    // The domain rows this surface reads, by name -- the same way a host feature option reads the row
    // named after it.
    private const string AuthChallengeName = "AuthChallenge";
    private const string ApiReferenceName = "ApiReference";

    // The store the host features are read from: server settings, not platform configuration.
    private static string ConfigurationStore => ServerConnection.Name;

    // Why the framework's own registration lives here rather than in each Program.cs: it is the
    // endpoint framework, and this is the collection of endpoints. Splitting the pair - Add in the
    // composition root, Use in a host - is what let them drift apart, and the app then built, started,
    // and threw on the first request looking for IServiceResolver.
    //
    // A different framework is a different collection that brings its own pair. Nothing above here
    // names FastEndpoints.
    /// <summary>Gets every group of endpoints the application serves — one per resource.</summary>
    /// <returns>The groups, in registration order.</returns>
    /// <remarks>
    /// Read through to <see cref="EndpointGroups"/> rather than held here: a group joins by declaring
    /// membership of that collection, which works across packages, where a parent/child attribute would
    /// emit a partial onto this type and cannot cross an assembly.
    /// </remarks>
    public static IReadOnlyCollection<IEndpointTypeCollection> Groups() => EndpointGroups.All();

    /// <summary>Gets the name the OpenAPI document is registered under.</summary>
    /// <remarks>
    /// Every API reference document endpoint generates from this registration. The document is never
    /// served as-is: each reference cuts its own copy for the caller (FDW-919).
    /// </remarks>
    public const string OpenApiDocumentName = "v1";

    static Endpoints()
    {
        // This collection's real membership is EndpointGroups, read through Groups() below -- its own
        // native [TypeOption]/[ServiceTypeOption] surface is never used and is empty by design,
        // forever, not because a package reference is missing. Without this, every phase logged the
        // generic "ran with NO options registered... entry point is missing a reference" warning on
        // every single boot, indistinguishable from the real failure it exists to catch.
        NativeMembersUnused = true;

        // Why appended rather than assigned: on this base the func IS the member cycle, so assigning
        // would replace it and nothing would register. Appending runs this after every member has
        // registered - which is required, not stylistic: AddFastEndpoints performs endpoint discovery
        // eagerly inside its own call, so every DeclaredEndpoints.Declare must already have happened.
        // Run it first and it throws 'unable to find any endpoint declarations'.
        Configuration((builder, loggerFactory) =>
        {
            foreach (var group in Groups())
            {
                var result = group.Configure(builder, loggerFactory);
                if (result.IsFailure)
                {
                    return result;
                }
            }

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {
            // Why the groups are cycled here rather than being this collection's own members: a group
            // joins EndpointGroups, which is membership and crosses packages. This collection is what
            // PlatformServices drives, so it is where that list gets walked.
            // Why each group is named as it registers: when nothing ends up declared, the useful
            // question is which group contributed nothing, and a silent loop cannot answer it.
            var registrationLogger = loggerFactory?.CreateLogger(nameof(Endpoints)) ?? NullLogger.Instance;
            EndpointRegistrationLog.EndpointGroupsJoined(registrationLogger, Groups().Count);

            foreach (var group in Groups())
            {
                var groupName = group.GetType().Name;
                var before = DeclaredEndpoints.Count;
                EndpointRegistrationLog.EndpointGroupRegistering(
                    registrationLogger, groupName, group.Members.Count());

                var result = group.Register(builder, loggerFactory);
                if (result.IsFailure)
                {
                    return result;
                }

                EndpointRegistrationLog.EndpointGroupContributed(
                    registrationLogger, groupName, DeclaredEndpoints.Count - before, DeclaredEndpoints.Count);
            }

            // Why no groups is a different answer from no endpoints: a host that joined no group
            // serves no REST endpoints, which a Blazor skin legitimately does not. Failing it would
            // be this collection insisting every host it runs in is an API. AddFastEndpoints is
            // skipped rather than called with nothing, and Initialize skips UseFastEndpoints to
            // match - splitting that pair is what produces "No service for type 'FastEndpoint...'".
            if (Groups().Count == 0)
            {
                EndpointRegistrationLog.NoEndpointGroups(
                    loggerFactory?.CreateLogger(nameof(Endpoints)) ?? NullLogger.Instance, "registration");
                return GenericResult<IHostApplicationBuilder>.Success(builder);
            }

            // Why the count is checked rather than left to fail later: with nothing declared,
            // AddFastEndpoints throws its own "unable to find any endpoint declarations", which says
            // nothing about which step went wrong. A host that joined a group and still declared
            // no endpoint has a broken registration chain, and this names it.
            if (DeclaredEndpoints.Count == 0)
            {
                return GenericResult<IHostApplicationBuilder>.Failure(
                    EndpointRegistrationLog.NoEndpointsDeclared(
                        loggerFactory?.CreateLogger(nameof(Endpoints)) ?? NullLogger.Instance));
            }

            builder.Services.AddFastEndpoints(o =>
            {
                // Why discovery is off and the types are handed over instead: an endpoint is
                // registered because its collection declared it. Scanning would add every endpoint
                // type in every loaded assembly regardless of whether a collection claims it, which
                // is the opposite of the switch these collections exist to provide.
                //
                // FastEndpoints offers no per-endpoint registration call. With auto-discovery off the
                // only way in is this list, which is why DeclaredEndpoints exists - each option adds
                // its own type as it registers, and this is where the collection gets passed across.
                // Filter is not used: it narrows what discovery found, and there is no discovery.
                o.DisableAutoDiscovery = true;
                o.SourceGeneratorDiscoveredTypes.AddRange(DeclaredEndpoints.Types);

                // Validators travel the same road. FastEndpoints reads them out of this same list, and
                // with discovery off nothing else puts them there — so every RuleFor in the codebase was
                // inert. That is why endpoints returned 201 for an empty body: the validator existed,
                // was never seen, and the handler ran anyway.
                //
                // Why the assemblies of declared endpoints and not a global scan: this stays bounded by
                // what an option actually declared. An assembly nobody registered an endpoint from is
                // not searched, so DisableAutoDiscovery still means what it says — nothing arrives that
                // was not asked for.
                foreach (var validatorType in DeclaredEndpoints.Types
                    .Select(t => t.Assembly)
                    .Distinct()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(FluentValidation.IValidator).IsAssignableFrom(t)))
                {
                    o.SourceGeneratorDiscoveredTypes.Add(validatorType);
                }
            });

            // Why these are registered here and not by the host: each is the other half of something
            // this collection already composes, and a host that mounts the pipeline without them gets
            // a surface that is wired but inert.
            //
            // UseProblemDetails is applied as a convention beside UseFastEndpoints, and covers
            // validation failures; the .NET producer covers what it does not — unhandled exceptions
            // and bare status codes — so a caller never has to branch on which layer refused it.
            builder.Services.AddProblemDetails();

            // UseRateLimiter is placed in the pipeline here too, and a limiter with no policies
            // registered lets every request through while looking configured.
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddFrameworkRateLimiting(loggerFactory ?? NullLoggerFactory.Instance);

            // The error envelope this surface returns carries the support contact, and the
            // middleware that writes it lives beside these options.
            // Why no ServiceEndpoints binding here: the proxy endpoints reach their peers through
            // named HTTP clients (ApiClients:{Client}:BaseUrl), never through a second copy of the
            // same addresses. ServiceEndpointsOptions bound that second copy and nothing ever read
            // it, so the two drifted four ports apart while the inert one looked authoritative.
            builder.Services.AddSingleton<IApiReferenceHostConfigurationProvider, ApiReferenceHostConfigurationProvider>(sp => new ApiReferenceHostConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<ApiReferenceHostConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConfigurationStore, "hst", "HostApiReferenceConfiguration"));

            // FDW supplies the ApiReference domain provider, not its registration: the host names the store
            // the domain reads and the implementation it dispatches to.
            builder.Services.TryAddSingleton(sp =>
            {
                var domain = new ApiReferenceConfigurationProvider(
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<ApiReferenceConfigurationProvider>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationStore, "hst", "ApiReference");
                domain.Register(HostImplementation, sp.GetRequiredService<IApiReferenceHostConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IApiReferenceConfigurationProvider>(sp => sp.GetRequiredService<ApiReferenceConfigurationProvider>());

            builder.Services.AddSingleton<IAuthChallengeHostConfigurationProvider, AuthChallengeHostConfigurationProvider>(sp => new AuthChallengeHostConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<AuthChallengeHostConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConfigurationStore, "hst", "HostAuthChallengeConfiguration"));

            // FDW supplies the AuthChallenge domain provider, not its registration: the host names the store
            // the domain reads and the implementation it dispatches to.
            builder.Services.TryAddSingleton(sp =>
            {
                var domain = new AuthChallengeConfigurationProvider(
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<AuthChallengeConfigurationProvider>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationStore, "hst", "AuthChallenge");
                domain.Register(HostImplementation, sp.GetRequiredService<IAuthChallengeHostConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IAuthChallengeConfigurationProvider>(sp => sp.GetRequiredService<AuthChallengeConfigurationProvider>());

            RegisterOpenApiDocument(builder, loggerFactory);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, loggerFactory) =>
        {
            foreach (var group in Groups())
            {
                var result = group.Initialize(host, loggerFactory);
                if (result.IsFailure)
                {
                    return result;
                }
            }

            // The other half of the pair above: nothing was added, so nothing is used.
            if (Groups().Count == 0)
            {
                EndpointRegistrationLog.NoEndpointGroups(
                    loggerFactory?.CreateLogger(nameof(Endpoints)) ?? NullLogger.Instance, "initialization");
                return GenericResult<IHost>.Success(host);
            }


            if (host is IApplicationBuilder app)
            {
                ComposeRequestPipeline(app, loggerFactory);
            }

            return GenericResult<IHost>.Success(host);
        });
    }

    // Why the OpenAPI document is registered here rather than in each host's Program.cs: the
    // processors describe endpoints, and this collection is what knows the endpoints.
    //
    // Nothing caller- or configuration-specific runs during generation (FDW-919). The document is
    // generated once per process and every API reference cuts its own copy per request, so a processor
    // that read the caller or the data sets would bake the first request's answer into every later one
    // - which is what NSwag's middleware did when it served the document itself. The operation
    // processor only stamps who may call each operation, from endpoint metadata that depends on the
    // build alone.
    //
    // AuthAndTagDocumentProcessor is deliberately NOT attached here: it requires a clientId and scope,
    // which are per-application values this collection has no non-arbitrary source for. A host that
    // wants it attaches it itself rather than having this invent a default.
    private static void RegisterOpenApiDocument(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory)
    {
        var title = builder.Environment.ApplicationName;
        var logger = loggerFactory?.CreateLogger(nameof(Endpoints)) ?? NullLogger.Instance;

        builder.Services.SwaggerDocument(o =>
        {
            // Why off: FastEndpoints' path tag is a second, differently cased group beside an endpoint's own, so a
            // reference listed most operations twice. OperationTagDocumentProcessor gives each one group (FDW-1100).
            o.AutoTagPathSegmentIndex = 0;
            o.DocumentSettings = s =>
            {
                s.DocumentName = OpenApiDocumentName;
                s.Title = title;
                s.OperationProcessors.Add(new AuthorizationMetadataOperationProcessor());
                s.DocumentProcessors.Add(new ValuesFromSchemaDocumentProcessor());
                s.DocumentProcessors.Add(new ConnectionConfigurationDocumentProcessor());
                s.DocumentProcessors.Add(new OperationTagDocumentProcessor());
            };
        });

        EndpointRegistrationLog.OpenApiProcessorAttached(logger, nameof(AuthorizationMetadataOperationProcessor), OpenApiDocumentName);
        EndpointRegistrationLog.OpenApiProcessorAttached(logger, nameof(ValuesFromSchemaDocumentProcessor), OpenApiDocumentName);
        EndpointRegistrationLog.OpenApiProcessorAttached(logger, nameof(ConnectionConfigurationDocumentProcessor), OpenApiDocumentName);
        EndpointRegistrationLog.OpenApiProcessorAttached(logger, nameof(OperationTagDocumentProcessor), OpenApiDocumentName);
        EndpointRegistrationLog.OpenApiProcessorsRegistered(
            logger,
            OpenApiDocumentName,
            4,
            $"{nameof(AuthorizationMetadataOperationProcessor)}, {nameof(ValuesFromSchemaDocumentProcessor)}, {nameof(ConnectionConfigurationDocumentProcessor)}, {nameof(OperationTagDocumentProcessor)}");
    }

    // Why this collection owns the sequence and not each host: the ORDER is invariant framework
    // knowledge — authentication before authorization, and the rate limiter after both so it sees an
    // identified caller — while the POSITION is the host's, expressed by where it invokes this phase.
    // Three hosts previously hand-wrote this block, each suppressing the collection's own
    // UseFastEndpoints to do it, which is three copies of one ordering that can drift independently.
    // Why blocking: composing a pipeline cannot await, and the row decides what this surface says.
    private static AuthChallengeHostConfiguration ReadAuthChallenge(IApplicationBuilder app)
    {
        var provider = app.ApplicationServices.GetRequiredService<IAuthChallengeConfigurationProvider>();
#pragma warning disable VSTHRD002
        var result = provider.Get(AuthChallengeName).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (result.IsFailure || result.Value is not AuthChallengeHostConfiguration configuration)
        {
            throw new InvalidOperationException(
                "No AuthChallenge row named '" + AuthChallengeName + "' is configured. What this surface tells a refused caller is "
                + "configuration rather than a default.");
        }

        return configuration;
    }

    // Why the Host domain and not a table of its own: support contact is not an implementation
    // choice the way Cors or SecurityHeaders is -- a host runs several of those at once, so "the"
    // implementation is never singular for a Host row -- it describes the host itself, the same way
    // Description already does. Looked up by this application's own name, the same identity
    // RegisterOpenApiDocument already uses for the OpenAPI document title.
    private static SupportContactInfo ReadSupportContact(IApplicationBuilder app)
    {
        var provider = app.ApplicationServices.GetRequiredService<IHostConfigurationProvider>();
        var applicationName = app.ApplicationServices.GetRequiredService<IHostEnvironment>().ApplicationName;
#pragma warning disable VSTHRD002
        var result = provider.Get(applicationName).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (result.IsFailure || result.Value is null)
        {
            throw new InvalidOperationException(
                $"No Host row named '{applicationName}' is configured. The contact this surface "
                + "names on an unhandled exception is configuration rather than a default.");
        }

        // The contract stays empty on purpose -- each host option declares its own settings -- so the
        // support contact is read off the Host domain's single implementation, by name.
        if (result.Value is not HostImplementationConfiguration host)
        {
            throw new InvalidOperationException(
                $"The Host row named '{applicationName}' resolved to "
                + $"'{result.Value.GetType().Name}' rather than its HostImplementation row.");
        }

        return new SupportContactInfo
        {
            Email = host.Email,
            Phone = host.Phone,
            PortalUrl = host.PortalUrl,
            Instructions = host.Instructions,
            ExpectedResponseTimeHours = host.ExpectedResponseTimeHours,
        };
    }

    // FDW-809: same Host domain row ReadSupportContact reads, read again here rather than threading
    // the already-read row through -- a second read is a cache hit through the same gateway, and
    // keeping this its own small read/check pair (matching ReadSupportContact's own shape) is a
    // smaller diff than restructuring both around one shared row.
    private static bool ReadServesRealTimeHubs(IApplicationBuilder app)
    {
        var provider = app.ApplicationServices.GetRequiredService<IHostConfigurationProvider>();
        var applicationName = app.ApplicationServices.GetRequiredService<IHostEnvironment>().ApplicationName;
#pragma warning disable VSTHRD002
        var result = provider.Get(applicationName).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (result.IsFailure || result.Value is not HostImplementationConfiguration host)
        {
            throw new InvalidOperationException(
                $"No Host row named '{applicationName}' is configured. Whether this host maps "
                + "real-time hub endpoints is configuration rather than a default.");
        }

        return host.ServesRealTimeHubs;
    }

    private static void ComposeRequestPipeline(IApplicationBuilder app, ILoggerFactory? loggerFactory)
    {
        var logger = loggerFactory?.CreateLogger(nameof(Endpoints)) ?? NullLogger.Instance;

        // Why each line follows its call rather than preceding it: the message states the middleware
        // IS in the pipeline. Logged first, a throw leaves a line claiming something that never happened.
        // The other half of AddProblemDetails above. That call says this surface answers validation
        // failures, unhandled exceptions and bare status codes in one shape; these two are what
        // deliver the second and third. Registered here and installed by each host, they drifted:
        // the registration claimed a contract the host had to remember to honour.
        var challenge = ReadAuthChallenge(app);
        app.UseStatusCodePages(async (StatusCodeContext statusContext) =>
        {
            var status = statusContext.HttpContext.Response.StatusCode;
            if (status is not (401 or 403)) return;
            if (statusContext.HttpContext.Response.HasStarted) return;
            statusContext.HttpContext.Response.ContentType = "application/json";
            await statusContext.HttpContext.Response.WriteAsJsonAsync(new
            {
                errorCode = status == 401 ? challenge.UnauthorizedCode : challenge.ForbiddenCode,
                messages = new[]
                {
                    status == 401 ? challenge.UnauthorizedMessage : challenge.ForbiddenMessage,
                },
            }).ConfigureAwait(false);
        });
        EndpointRegistrationLog.PipelineMiddlewareAdded(
            logger, 1, "UseStatusCodePages", "gives a bare 401 or 403 the same JSON shape as every other refusal");

        // Why the environment and not a row: this preserves what reference-api ran. Whether a host
        // sends Strict-Transport-Security is configuration rather than an environment name, and
        // changing that is its own change -- see API-151.
        if (app.ApplicationServices.GetService<IHostEnvironment>() is { } env &&
            !env.IsDevelopment())
        {
            app.UseHsts();
            EndpointRegistrationLog.PipelineMiddlewareAdded(
                logger, 2, "UseHsts", "nothing else on this surface sends Strict-Transport-Security");
        }

        app.UseMiddleware<GlobalExceptionHandlerMiddleware>(ReadSupportContact(app));
        EndpointRegistrationLog.PipelineMiddlewareAdded(
            logger, 3, "GlobalExceptionHandlerMiddleware", "the unhandled-exception half of the same envelope");

        app.UseHttpsRedirection();
        EndpointRegistrationLog.PipelineMiddlewareAdded(
            logger, 4, "UseHttpsRedirection", "before anything reads the scheme or sets a secure cookie");

        app.UseSerilogRequestLogging();
        EndpointRegistrationLog.PipelineMiddlewareAdded(
            logger, 5, "UseSerilogRequestLogging", "one completion line per request, outside the handlers it times");

        app.UseAuthentication();
        EndpointRegistrationLog.PipelineMiddlewareAdded(
            logger, 6, "UseAuthentication", "establishes the caller before anything reads their identity");

        app.UseAuthorization();
        EndpointRegistrationLog.PipelineMiddlewareAdded(
            logger, 7, "UseAuthorization", "reads the identity authentication just established");

        app.UseMiddleware<RequestContextMiddleware>();
        EndpointRegistrationLog.PipelineMiddlewareAdded(
            logger, 8, "RequestContextMiddleware",
            "turns the principal authentication established into the ambient context the data plane reads");

        // Why the option's own flag decides this and not a package reference to a provider: a host
        // that joined no multitenancy option has no tenant to resolve, and EnablesTenantResolution is
        // the option's own answer to whether it does. The middleware read claims and headers through
        // the Multitenancy abstractions, so placing them here names no storage implementation.
        if (app.ApplicationServices.GetService<IMultitenancyType>() is { EnablesTenantResolution: true })
        {
            app.UseMiddleware<TenantResolutionMiddleware>();
            EndpointRegistrationLog.PipelineMiddlewareAdded(
                logger, 9, "TenantResolutionMiddleware",
                "resolves the tenant from the claims authentication established");

            app.UseMiddleware<OrgResolutionMiddleware>();
            EndpointRegistrationLog.PipelineMiddlewareAdded(
                logger, 10, "OrgResolutionMiddleware",
                "resolves the org within the tenant the previous step established");
        }

        app.UseRateLimiter();
        EndpointRegistrationLog.PipelineMiddlewareAdded(
            logger, 11, "UseRateLimiter", "placed after all of them so a policy can key on an identified caller");

        // Mapped BEFORE UseFastEndpoints, and deliberately not as a FastEndpoint: if it were one it
        // would die with the thing it reports on, which is the whole failure this exists to name. A
        // route whose mapping threw and a route that was never built are the same zero-byte 404 from
        // outside the host -- react-ui-design's console told a first-time visitor the workspace API
        // "was never built" while it was fully built, registered and deployed. See API-163.
        //
        // Anonymous because a client that cannot authenticate still needs to know whether the auth
        // endpoint exists. It exposes no route names -- only whether mapping finished, and how many.
        if (app is WebApplication statusHost)
        {
            statusHost.MapGet("/endpoints/status", () => Results.Json(new
            {
                mappingCompleted = EndpointMappingStatus.MappingCompleted,
                endpointCount = EndpointMappingStatus.EndpointCount,
            })).AllowAnonymous();
        }

        app.UseFastEndpoints(config =>
        {
            config.Endpoints.RoutePrefix = RoutePrefix;
            EndpointRegistrationLog.EndpointConventionApplied(
                logger, 1, "Endpoints.RoutePrefix", RoutePrefix, "the version prefix every FDW service surface is served under");

            config.Security.RoleClaimType = RoleClaimType;
            EndpointRegistrationLog.EndpointConventionApplied(
                logger, 2, "Security.RoleClaimType", RoleClaimType,
                "FDW bakes role names under this claim; at the framework default every role-gated endpoint denies while the token carries the role");

            config.Errors.UseProblemDetails();
            EndpointRegistrationLog.EndpointConventionApplied(
                logger, 3, "Errors", "ProblemDetails", "the error shape every FDW service surface returns");

            // Why the collection attaches this and not each endpoint base: a Policies("resource:action")
            // declaration that nothing checks FAILS OPEN, so the check cannot depend on an endpoint
            // deriving from the right base or a host remembering the call.
            config.Endpoints.Configurator = ep => ep.PreProcessors(Order.Before, new PermissionClaimsPreProcessor());
            EndpointRegistrationLog.EndpointConventionApplied(
                logger, 4, "Endpoints.Configurator", nameof(PermissionClaimsPreProcessor),
                "checks each endpoint's declared permission; without it Policies(resource:action) fails open");

            EndpointRegistrationLog.PermissionPreProcessorAttached(logger);

            config.Serializer.RequestDeserializer = (request, requestType, jsonContext, ct) =>
                BodylessRequestDeserializer.Read(request, requestType, jsonContext, config.Serializer.Options, ct);
            EndpointRegistrationLog.EndpointConventionApplied(
                logger, 5, "Serializer.RequestDeserializer", nameof(BodylessRequestDeserializer),
                "a GET, HEAD or DELETE that names a JSON content type but carries no body is an empty request, not a 400 (FDW-1030)");
        });
        EndpointRegistrationLog.PipelineMiddlewareAdded(
            logger, 4, "UseFastEndpoints", "endpoints run last, once the caller is established and limited");

        // Set only on the normal return. If UseFastEndpoints throws, this line never runs and the
        // flag stays false, which is exactly the state a caller needs to be able to read.
        EndpointMappingStatus.MarkCompleted(DeclaredEndpoints.Count);

        // FDW-809: every real-time hub option loads into every host's DI container identically --
        // the package graph does not distinguish api/etl/scheduler here, so which host actually maps
        // hub connection routes is a fact the Host row carries, the same way it already carries the
        // support contact read above. RealTimeHubs.Map is itself idempotent, but the gate still lives
        // here so a host that should never accept hub connections never calls it at all.
        if (app is WebApplication hubHost && ReadServesRealTimeHubs(app))
        {
            RealTimeHubs.Map(hubHost, loggerFactory);
            EndpointRegistrationLog.PipelineMiddlewareAdded(
                logger, 12, "RealTimeHubs.Map", "this host's Host row declares ServesRealTimeHubs=1");
        }

        if (app is WebApplication web)
        {
            ScalarDocumentation.Map(web, ReadApiReference(web), loggerFactory);
        }

        EndpointRegistrationLog.PipelineComposed(
            logger, "UseAuthentication -> UseAuthorization -> UseRateLimiter -> UseFastEndpoints");
    }

    // Why private: a host no longer applies these itself. The conventions are applied to the
    // UseFastEndpoints call this collection makes, so there is no way to wire endpoints and miss them
    // — which is what happened when the framework called UseFastEndpoints with no config at all and
    // every host had to re-issue it to get the route prefix and the roles claim type back.

    /// <summary>Gets the claim type authorization reads role names from.</summary>
    /// <remarks>FDW bakes role names under the plural "roles" claim; the framework default finds none.</remarks>
    public static string RoleClaimType => "roles";

    /// <summary>Gets the version prefix every FDW service surface is served under.</summary>
    /// <remarks>Read this rather than hardcoding the prefix when building a URL to a peer.</remarks>
    public static string RoutePrefix => "api/v1";

    // Why blocking: a phase body cannot await, and the page cannot be served without knowing where.
    private static ApiReferenceHostConfiguration ReadApiReference(WebApplication app)
    {
        var provider = app.Services.GetRequiredService<IApiReferenceConfigurationProvider>();
#pragma warning disable VSTHRD002
        var result = provider.Get(ApiReferenceName).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (result.IsFailure || result.Value is not ApiReferenceHostConfiguration configuration)
        {
            throw new InvalidOperationException(
                "No ApiReference row named '" + ApiReferenceName + "' is configured. Where the reference page is served, and what it "
                + "shows, is configuration rather than a default.");
        }

        return configuration;
    }
}
