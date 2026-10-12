using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Web.RestEndpoints.ApiReference;
using Microsoft.Extensions.Logging;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint for the API reference of one workspace.</summary>
/// <remarks>Behaviour lives in the base; this gives its <c>[TypeOption]</c> a type to name and names the document it cuts.</remarks>
[ExcludeFromCodeCoverage]
public sealed class WorkspaceReferenceDocumentEndpoint : WorkspaceReferenceDocumentEndpointBase
{
    /// <inheritdoc />
    public WorkspaceReferenceDocumentEndpoint(
        ILogger<ApiReferenceDocumentEndpointBase<WorkspaceReferenceRequest>> logger,
        ISystemRoleConfiguration systemRoles,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IDataSetConfigurationProvider dataSets)
        : base(logger, systemRoles, workspaces, access, dataSets)
    {
    }

    /// <inheritdoc />
    protected override string DocumentName => FdwReferenceEndpoints.Endpoints.OpenApiDocumentName;
}
