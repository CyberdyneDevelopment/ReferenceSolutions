using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint to change a member's role on a workspace.</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class UpdateWorkspaceMemberRoleEndpoint : UpdateWorkspaceMemberRoleEndpointBase
{
    /// <inheritdoc />
    public UpdateWorkspaceMemberRoleEndpoint(
        ILogger<UpdateWorkspaceMemberRoleEndpointBase> logger,
        WorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access)
        : base(logger, workspaces, access)
    {
    }
}
