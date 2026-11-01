using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint to read one workspace with its members, resources and relationships.</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class GetWorkspaceEndpoint : GetWorkspaceEndpointBase
{
    /// <inheritdoc />
    public GetWorkspaceEndpoint(
        ILogger<GetWorkspaceEndpointBase> logger,
        WorkspaceConfigurationProvider provider,
        IWorkspaceAccessPolicy access)
        : base(logger, provider, access)
    {
    }
}
