using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint to remove a member from a workspace.</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class RemoveWorkspaceMemberEndpoint : RemoveWorkspaceMemberEndpointBase
{
    /// <inheritdoc />
    public RemoveWorkspaceMemberEndpoint(
        WorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        ILogger<RemoveWorkspaceMemberEndpointBase>? logger = null)
        : base(workspaces, access, logger)
    {
    }
}
