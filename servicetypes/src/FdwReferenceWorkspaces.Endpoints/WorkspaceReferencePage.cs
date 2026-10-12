using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Workspaces;
using Fdw.Web.RestEndpoints.ApiReference;
using FdwReferenceEndpoints.ApiReference;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>
/// One workspace: <c>/api/v1/{WorkspaceName}/scalar</c>, reading <c>/api/v1/openapi/data/{WorkspaceName}</c>.
/// </summary>
/// <remarks>
/// <para>
/// At the root beside the data route <c>/{WorkspaceName}/{DataSetName}</c> (FDW-969), not under
/// <c>/data/</c>. A literal route segment beats a parameter, so a workspace named after a resource
/// with a GET-by-name route (<c>connections</c>, <c>datasets</c>) or after another reference page
/// (<c>data</c>, <c>reference</c>) is answered by that route instead and has no reference page.
/// </para>
/// <para>
/// Offers one link per workspace the caller may read, by the same rule the data route applies. A
/// workspace the caller may not read is not linked, and its document answers 404 as the route does.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ApiReferencePages), "WorkspaceReference")]
public sealed class WorkspaceReferencePage : ApiReferencePageBase
{
    private const string WorkspaceRouteValue = "WorkspaceName";

    /// <summary>Initializes a new instance of the <see cref="WorkspaceReferencePage"/> class.</summary>
    public WorkspaceReferencePage()
        : base(
            "WorkspaceReference",
            "/" + FdwReferenceEndpoints.Endpoints.RoutePrefix + "/{" + WorkspaceRouteValue + "}/scalar",
            "Workspace",
            requiresSignIn: true,
            requiredPolicies: [],
            headerContent: ReferencePagesNav.Markup)
    {
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The request did not come through this page's route.</exception>
    public override string DocumentUrl(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Request.RouteValues[WorkspaceRouteValue] is not string workspaceName)
        {
            throw new InvalidOperationException(
                "The workspace reference page was served without a '" + WorkspaceRouteValue + "' route value; its route is " + Route + ".");
        }

        return DocumentUrlFor(workspaceName);
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<IReadOnlyList<ApiReferenceLink>>> Links(
        ApiReferenceCaller caller, HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!Admits(caller, context))
            return GenericResult<IReadOnlyList<ApiReferenceLink>>.Success([]);

        var readable = await ReadableWorkspaces.Get(
            context.RequestServices.GetRequiredService<IWorkspaceConfigurationProvider>(),
            context.RequestServices.GetRequiredService<IWorkspaceAccessPolicy>(),
            cancellationToken).ConfigureAwait(false);
        if (readable.IsFailure || readable.Value is not { } workspaces)
            return readable.ToNewResult<IReadOnlyList<ApiReferenceLink>>();

        // A name is unique within a tenant only, so a caller who sees several tenants can read two workspaces of one
        // name. Each still gets its own entry, labelled with its tenant; its name-addressed document then answers the
        // ambiguity with its reason instead of silently serving one of them (FDW-1101).
        var shared = workspaces
            .GroupBy(workspace => workspace.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Skip(1).Any())
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<ApiReferenceLink> links =
        [
            .. workspaces.Select(workspace => new ApiReferenceLink(
                shared.Contains(workspace.Name)
                    ? "Workspace: " + workspace.Name + " (tenant " + workspace.TenantId?.ToString("D", CultureInfo.InvariantCulture) + ")"
                    : "Workspace: " + workspace.Name,
                "/" + FdwReferenceEndpoints.Endpoints.RoutePrefix + "/" + Uri.EscapeDataString(workspace.Name) + "/scalar")),
        ];
        return GenericResult<IReadOnlyList<ApiReferenceLink>>.Success(links);
    }

    private static string DocumentUrlFor(string workspaceName)
        => "/" + FdwReferenceEndpoints.Endpoints.RoutePrefix + "/openapi/data/" + Uri.EscapeDataString(workspaceName);
}
