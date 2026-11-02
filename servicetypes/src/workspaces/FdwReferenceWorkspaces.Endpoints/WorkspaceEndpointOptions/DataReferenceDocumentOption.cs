using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The API reference document of every readable workspace — GET /openapi/data.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceEndpoints), "DataReferenceDocument")]
public class DataReferenceDocumentOption : WorkspaceEndpointBase<DataReferenceDocumentEndpoint>
{
}
