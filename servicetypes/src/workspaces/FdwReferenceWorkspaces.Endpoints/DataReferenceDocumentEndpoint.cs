using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Web.RestEndpoints.ApiReference;
using Microsoft.Extensions.Logging;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint for the API reference of every workspace the caller may read.</summary>
/// <remarks>Behaviour lives in the base; this gives its <c>[TypeOption]</c> a type to name and names the document it cuts.</remarks>
[ExcludeFromCodeCoverage]
public sealed class DataReferenceDocumentEndpoint : DataReferenceDocumentEndpointBase
{
    /// <inheritdoc />
    public DataReferenceDocumentEndpoint(
        ILogger<ApiReferenceDocumentEndpointBase<EmptyRequest>> logger,
        ISystemRoleConfiguration systemRoles,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IWorkspaceReadService readService)
        : base(logger, systemRoles, workspaces, access, readService)
    {
    }

    /// <inheritdoc />
    protected override string DocumentName => FdwReferenceEndpoints.Endpoints.OpenApiDocumentName;
}
