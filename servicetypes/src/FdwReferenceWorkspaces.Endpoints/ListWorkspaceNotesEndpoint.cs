using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Users;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint to list a workspace's notes.</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ListWorkspaceNotesEndpoint : ListWorkspaceNotesEndpointBase
{
    /// <inheritdoc />
    public ListWorkspaceNotesEndpoint(
        ILogger<ListWorkspaceNotesEndpointBase> logger,
        WorkspaceConfigurationProvider workspaces,
        NoteConfigurationProvider notes,
        UserConfigurationProvider users,
        IDataSetConfigurationProvider dataSets,
        IWorkspaceAccessPolicy access)
        : base(logger, workspaces, notes, users, dataSets, access)
    {
    }
}
