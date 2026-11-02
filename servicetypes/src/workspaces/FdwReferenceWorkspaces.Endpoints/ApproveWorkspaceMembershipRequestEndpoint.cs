using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint to approve a membership request.</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ApproveWorkspaceMembershipRequestEndpoint : ApproveWorkspaceMembershipRequestEndpointBase
{
    /// <inheritdoc />
    public ApproveWorkspaceMembershipRequestEndpoint(
        WorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IAuthenticationContextAccessor authContext,
        ILogger<ApproveWorkspaceMembershipRequestEndpointBase>? logger = null)
        : base(workspaces, access, authContext, logger)
    {
    }
}
