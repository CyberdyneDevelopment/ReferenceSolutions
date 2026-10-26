using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The DeleteGlossaryTermVersion endpoint (FDW-2331).</summary>
[WebMcpTool("delete_glossary_term_version", "Delete a version of a glossary term from an organization or workspace.")]
[TypeOption(typeof(CatalogEndpoints), "DeleteGlossaryTermVersion")]
public class DeleteGlossaryTermVersionOption : CatalogEndpointBase<DeleteGlossaryTermVersionEndpoint>
{
}
