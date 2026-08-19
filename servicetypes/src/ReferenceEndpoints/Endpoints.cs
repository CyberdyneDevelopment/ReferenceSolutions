using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.RestEndpoints.Logging;
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

namespace ReferenceEndpoints;

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

    // Why these are held rather than constructed twice: the Register phase attaches the instance to
    // the OpenAPI document settings, and the Initialize phase must hand that SAME instance the built
    // service provider. Two constructions would attach one object and initialize another, leaving the
    // document holding a processor whose provider is null — which no-ops silently.
    private static PermissionFilterDocumentProcessor? PermissionFilter { get; set; }

    private static DataSetQueryDocumentProcessor? DataSetQuery { get; set; }

    static Endpoints()
    {
        // Why appended rather than assigned: on this base the func IS the member cycle, so assigning
        // would replace it and nothing would register. Appending runs this after every member has
        // registered - which is required, not stylistic: AddFastEndpoints performs endpoint discovery
        // eagerly inside its own call, so every DeclaredEndpoints.Declare must already have happened.
        // Run it first and it throws 'unable to find any endpoint declarations'.
        AppendConfiguration((builder, loggerFactory) =>
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

        AppendRegistration((builder, loggerFactory) =>
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

            // Why the accessor is registered HERE and not in the host: it exists for
            // PermissionFilterDocumentProcessor below, which reads the caller's claims through it at
            // document-generation time. Registered beside the processor that needs it, deleting
            // either one is visibly deleting half a pair.
            builder.Services.AddHttpContextAccessor();

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

            // The error envelope this surface returns carries the support contact, and the proxy
            // endpoints read their peers' addresses from these. Both option types live beside the
            // middleware that reads them.
            builder.Services.Configure<ReferenceEndpoints.Models.SupportOptions>(
                builder.Configuration.GetSection("Support"));
            builder.Services.Configure<ReferenceEndpoints.Configuration.ServiceEndpointsOptions>(
                builder.Configuration.GetSection(ReferenceEndpoints.Configuration.ServiceEndpointsOptions.SectionName));

            RegisterOpenApiDocument(builder, loggerFactory);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        AppendInitialization((host, loggerFactory) =>
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

            InitializeOpenApiProcessors(host, loggerFactory);

            if (host is IApplicationBuilder app)
            {
                ComposeRequestPipeline(app, loggerFactory);
            }

            return GenericResult<IHost>.Success(host);
        });
    }

    // Why the OpenAPI document is built here rather than in each host's Program.cs: these processors
    // describe endpoints, and this collection is what knows the endpoints. Left to the host, the
    // SwaggerDocument call carried only a title and no processors at all — so the permission filter
    // and the dataset injection never ran, and the document listed every operation in the app to
    // anonymous callers. Nothing reported it, because a processor that is never attached does not
    // fail; it is simply absent.
    //
    // Why the two stateful processors are kept: they cannot be constructed with a service provider
    // (none exists before Build), so they take one in Initialize. Holding the instances is the only
    // way the Initialize phase can reach the same objects the document holds.
    //
    // AuthAndTagDocumentProcessor is deliberately NOT attached here: it requires a clientId and scope,
    // which are per-application values this collection has no non-arbitrary source for. A host that
    // wants it attaches it itself rather than having this invent a default.
    private static void RegisterOpenApiDocument(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory)
    {
        var documentName = builder.Environment.ApplicationName;
        var logger = loggerFactory?.CreateLogger(nameof(Endpoints)) ?? NullLogger.Instance;

        PermissionFilter = new PermissionFilterDocumentProcessor();
        DataSetQuery = new DataSetQueryDocumentProcessor();
        var valuesFromSchema = new ValuesFromSchemaDocumentProcessor();

        builder.Services.SwaggerDocument(o =>
        {
            o.DocumentSettings = s =>
            {
                s.Title = documentName;
                s.DocumentProcessors.Add(PermissionFilter);
                s.DocumentProcessors.Add(DataSetQuery);
                s.DocumentProcessors.Add(valuesFromSchema);
            };
        });

        EndpointRegistrationLog.OpenApiProcessorAttached(logger, nameof(PermissionFilterDocumentProcessor), documentName);
        EndpointRegistrationLog.OpenApiProcessorAttached(logger, nameof(DataSetQueryDocumentProcessor), documentName);
        EndpointRegistrationLog.OpenApiProcessorAttached(logger, nameof(ValuesFromSchemaDocumentProcessor), documentName);
        EndpointRegistrationLog.OpenApiProcessorsRegistered(
            logger,
            documentName,
            3,
            $"{nameof(PermissionFilterDocumentProcessor)}, {nameof(DataSetQueryDocumentProcessor)}, {nameof(ValuesFromSchemaDocumentProcessor)}");
    }

    // Why this is a separate phase and not part of Register: both processors read services
    // (IHttpContextAccessor for the caller's claims, the dataset providers for the schema) at
    // document-generation time, and neither can be handed a provider before Build. Their Process()
    // opens with a null-provider guard and returns silently, so skipping this step does not throw —
    // the document just comes out unfiltered, which is exactly the failure that hid here before.
    // Why this collection owns the sequence and not each host: the ORDER is invariant framework
    // knowledge — authentication before authorization, and the rate limiter after both so it sees an
    // identified caller — while the POSITION is the host's, expressed by where it invokes this phase.
    // Three hosts previously hand-wrote this block, each suppressing the collection's own
    // UseFastEndpoints to do it, which is three copies of one ordering that can drift independently.
    private static void ComposeRequestPipeline(IApplicationBuilder app, ILoggerFactory? loggerFactory)
    {
        var logger = loggerFactory?.CreateLogger(nameof(Endpoints)) ?? NullLogger.Instance;

        // Why each line follows its call rather than preceding it: the message states the middleware
        // IS in the pipeline. Logged first, a throw leaves a line claiming something that never happened.
        app.UseAuthentication();
        EndpointRegistrationLog.PipelineMiddlewareAdded(
            logger, 1, "UseAuthentication", "establishes the caller before anything reads their identity");

        app.UseAuthorization();
        EndpointRegistrationLog.PipelineMiddlewareAdded(
            logger, 2, "UseAuthorization", "reads the identity authentication just established");

        app.UseRateLimiter();
        EndpointRegistrationLog.PipelineMiddlewareAdded(
            logger, 3, "UseRateLimiter", "placed after both so a policy can key on an identified caller");

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
        });
        EndpointRegistrationLog.PipelineMiddlewareAdded(
            logger, 4, "UseFastEndpoints", "endpoints run last, once the caller is established and limited");

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

    private static void InitializeOpenApiProcessors(IHost host, ILoggerFactory? loggerFactory)
    {
        var logger = loggerFactory?.CreateLogger(nameof(Endpoints)) ?? NullLogger.Instance;

        if (PermissionFilter is null && DataSetQuery is null)
        {
            EndpointRegistrationLog.OpenApiProcessorsMissing(logger);
            return;
        }

        if (PermissionFilter is not null)
        {
            PermissionFilter.Initialize(host.Services);
            EndpointRegistrationLog.OpenApiProcessorInitialized(logger, nameof(PermissionFilterDocumentProcessor));
        }

        if (DataSetQuery is not null)
        {
            DataSetQuery.Initialize(host.Services);
            EndpointRegistrationLog.OpenApiProcessorInitialized(logger, nameof(DataSetQueryDocumentProcessor));
        }
    }
}
