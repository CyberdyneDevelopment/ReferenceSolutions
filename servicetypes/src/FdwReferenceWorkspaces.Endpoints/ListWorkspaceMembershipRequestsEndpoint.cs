using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint to list a workspace's membership requests.</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ListWorkspaceMembershipRequestsEndpoint : ListWorkspaceMembershipRequestsEndpointBase
{
    /// <inheritdoc />
    public ListWorkspaceMembershipRequestsEndpoint(
        ILogger<ListWorkspaceMembershipRequestsEndpointBase> logger,
        WorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access)
        : base(logger, workspaces, access)
    {
    }
}
