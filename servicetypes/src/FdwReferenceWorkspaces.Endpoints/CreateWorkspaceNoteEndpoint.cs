using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Users;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint to write a note in a workspace.</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class CreateWorkspaceNoteEndpoint : CreateWorkspaceNoteEndpointBase
{
    /// <inheritdoc />
    public CreateWorkspaceNoteEndpoint(
        ILogger<CreateWorkspaceNoteEndpointBase> logger,
        WorkspaceConfigurationProvider workspaces,
        NoteConfigurationProvider notes,
        UserConfigurationProvider users,
        IAuthenticationContextAccessor authContext,
        IDataSetConfigurationProvider dataSets,
        IWorkspaceAccessPolicy access)
        : base(logger, workspaces, notes, users, authContext, dataSets, access)
    {
    }
}
