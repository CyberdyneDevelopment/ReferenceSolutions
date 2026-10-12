using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Fdw.Services.Authorization;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Abstractions;
﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Workspaces;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using Microsoft.Extensions.Hosting;
using FdwReferenceEndpoints;
using Microsoft.Extensions.DependencyInjection;
using Fdw.Services.Data;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>
/// This app's workspace endpoints. Membership of this collection IS the route table — an endpoint
/// class with no option here compiles and 404s.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "WorkspaceEndpoints")]
[TypeCollection(typeof(WorkspaceEndpointBase), typeof(IEndpointTypeOption), typeof(WorkspaceEndpoints))]
public partial class WorkspaceEndpoints : EndpointTypeCollectionBase<WorkspaceEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceEndpoints"/> class, registering the
    /// configuration providers its endpoints resolve.
    /// </summary>
    /// <remarks>
    /// Without this the routes load and every one of them fails to construct: the endpoints take
    /// WorkspaceConfigurationProvider, and nothing else in the host registers it. That failure is a
    /// 500 at call time rather than a 404, so it looks like a broken endpoint rather than an
    /// unregistered service — the group that declares the routes is therefore the right place to
    /// register what they depend on, which is what DataLineageEndpoints does for its own provider.
    /// </remarks>
    public WorkspaceEndpoints()
        => Registration((builder, loggerFactory) =>
        {
            builder.Services.AddSingleton<IWorkspaceImplementationConfigurationProvider, WorkspaceImplementationConfigurationProvider>(sp => new WorkspaceImplementationConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<WorkspaceImplementationConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConfigurationGatewayTypes.ConfigurationConnection, "workspace", "WorkspaceConfiguration"));

            builder.Services.TryAddSingleton<WorkspaceConfigurationProvider>(sp =>
            {
                var domain = new WorkspaceConfigurationProvider(
                    sp.GetRequiredService<ILogger<WorkspaceConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationGatewayTypes.ConfigurationConnection, "workspace", "Workspace");
                domain.Register("Workspace", sp.GetRequiredService<IWorkspaceImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IWorkspaceConfigurationProvider>(sp => sp.GetRequiredService<WorkspaceConfigurationProvider>());

            builder.Services.AddSingleton<INoteImplementationConfigurationProvider, NoteImplementationConfigurationProvider>(sp => new NoteImplementationConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<NoteImplementationConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                ConfigurationGatewayTypes.ConfigurationConnection, "workspace", "NoteConfiguration"));

            builder.Services.TryAddSingleton<NoteConfigurationProvider>(sp =>
            {
                var domain = new NoteConfigurationProvider(
                    sp.GetRequiredService<ILogger<NoteConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationGatewayTypes.ConfigurationConnection, "workspace", "Note");
                domain.Register("Note", sp.GetRequiredService<INoteImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<INoteConfigurationProvider>(sp => sp.GetRequiredService<NoteConfigurationProvider>());


            // Scoped, unlike the providers above: it reads the calling user off the ambient
            // authentication context, so it answers per request rather than once per process.
            builder.Services.TryAddScoped<IWorkspaceAccessPolicy>(sp =>
                new WorkspaceAccessPolicy(
                    sp.GetRequiredService<IAuthenticationContextAccessor>(),
                    sp.GetRequiredService<IRoleConfigurationProvider>(),
                    sp.GetRequiredService<IDataSetConfigurationProvider>(),
                    sp.GetService<ILogger<WorkspaceAccessPolicy>>()));

            // What a caller may read through workspaces, asked by every way of reading through one -- the
            // HTTP query route and a SQL Server connection alike. Scoped like the policy it asks.
            builder.Services.TryAddScoped<IWorkspaceReadService>(sp =>
                new WorkspaceReadService(
                    sp.GetRequiredService<IWorkspaceConfigurationProvider>(),
                    sp.GetRequiredService<IWorkspaceAccessPolicy>(),
                    sp.GetRequiredService<IDataSetConfigurationProvider>(),
                    sp.GetRequiredService<IDataSetVariationConfigurationProvider>(),
                    sp.GetService<ILogger<WorkspaceReadService>>()));

            // A workspace's own permissions (workspaces/{id}:view/query), created with it and removed
            // with it (FDW-937). Scoped like the policy: the providers are singletons, the work is per request.
            builder.Services.TryAddScoped<IWorkspacePermissions>(sp =>
                new WorkspacePermissions(
                    sp.GetRequiredService<IPermissionConfigurationProvider>(),
                    sp.GetRequiredService<IRolePermissionConfigurationProvider>(),
                    sp.GetService<ILogger<WorkspacePermissions>>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
}
