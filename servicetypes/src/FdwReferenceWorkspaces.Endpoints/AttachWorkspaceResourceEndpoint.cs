using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint to attach a resource to a workspace.</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class AttachWorkspaceResourceEndpoint : AttachWorkspaceResourceEndpointBase
{
    /// <inheritdoc />
    public AttachWorkspaceResourceEndpoint(
        ILogger<AttachWorkspaceResourceEndpointBase> logger,
        WorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IDataSetConfigurationProvider dataSets,
        IDataSetVariationConfigurationProvider variations,
        IAuthenticationContextAccessor authContext)
        : base(logger, workspaces, access, dataSets, variations, authContext)
    {
    }
}
