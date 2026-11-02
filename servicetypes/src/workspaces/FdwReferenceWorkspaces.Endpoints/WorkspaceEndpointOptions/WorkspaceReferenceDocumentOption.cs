using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The API reference document of one workspace — GET /openapi/data/{WorkspaceName}.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceEndpoints), "WorkspaceReferenceDocument")]
public class WorkspaceReferenceDocumentOption : WorkspaceEndpointBase<WorkspaceReferenceDocumentEndpoint>
{
}
