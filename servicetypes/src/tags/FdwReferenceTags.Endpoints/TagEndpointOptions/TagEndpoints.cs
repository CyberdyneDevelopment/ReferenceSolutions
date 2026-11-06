using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Tags;
using Fdw.Services.Workspaces;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FdwReferenceTags.Endpoints.TagEndpointOptions;

/// <summary>
/// This app's tag endpoints. Membership of this collection IS the route table: an endpoint class with no option here compiles and 404s.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "TagEndpoints")]
[TypeCollection(typeof(TagEndpointBase), typeof(IEndpointTypeOption), typeof(TagEndpoints))]
public partial class TagEndpoints : EndpointTypeCollectionBase<TagEndpointBase>
{
    private const string TagPath = "tag";

    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

    /// <summary>
    /// Initializes a new instance of the <see cref="TagEndpoints"/> class, registering the tag providers and the Tags service its endpoints resolve.
    /// </summary>
    /// <remarks>
    /// Without this the routes load and every one of them fails to construct, as a 500 at call time. The group that declares the routes
    /// registers what they depend on, as the workspace endpoints do. The scope rule reads workspaces, and the workspace provider is registered by the
    /// workspace endpoints, which only the API maps: the ETL host and the scheduler run this group so their lists answer <c>?tag=</c>, and there the
    /// scope rule is <see cref="UnavailableTagScope"/>, which refuses to put a tag on a thing instead of failing the service's construction. The owners list
    /// follows the same rule: <see cref="TagOwners"/> where the tenant context and the organization and workspace providers exist, <see cref="UnavailableTagOwners"/> elsewhere.
    /// It is scoped, not a singleton, because the tenant context is scoped to the request.
    /// </remarks>
    public TagEndpoints()
        => Registration((builder, loggerFactory) =>
        {
            builder.Services.AddSingleton<ITagImplementationConfigurationProvider, TagImplementationConfigurationProvider>(sp => new TagImplementationConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<TagImplementationConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConfigurationGatewayTypes.ConfigurationConnection, TagPath, "TagConfiguration"));

            builder.Services.TryAddSingleton<TagConfigurationProvider>(sp =>
            {
                var domain = new TagConfigurationProvider(
                    sp.GetRequiredService<ILogger<TagConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationGatewayTypes.ConfigurationConnection, TagPath, "Tag");
                domain.Register("Tag", sp.GetRequiredService<ITagImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<ITagConfigurationProvider>(sp => sp.GetRequiredService<TagConfigurationProvider>());

            builder.Services.TryAddSingleton<ITagScope>(sp => sp.GetService<IWorkspaceConfigurationProvider>() is { } workspaces
                ? new TagScope(workspaces, sp.GetService<ILogger<TagScope>>())
                : new UnavailableTagScope(sp.GetService<ILogger<UnavailableTagScope>>()));

            builder.Services.TryAddScoped<ITagOwners>(sp => sp.GetService<ITenantContext>() is { } tenant
                && sp.GetService<IOrganizationConfigurationProvider>() is { } organizations
                && sp.GetService<IWorkspaceConfigurationProvider>() is { } workspaces
                ? new TagOwners(tenant, organizations, workspaces, sp.GetService<ILogger<TagOwners>>())
                : new UnavailableTagOwners(sp.GetService<ILogger<UnavailableTagOwners>>()));

            builder.Services.TryAddSingleton<ITagService>(sp => new TagService(
                sp.GetRequiredService<ITagConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                sp.GetRequiredService<ITagScope>(),
                ConfigurationGatewayTypes.ConfigurationConnection,
                sp.GetService<ILogger<TagService>>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
}
