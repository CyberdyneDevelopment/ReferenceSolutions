using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The DeleteGlossaryTerm endpoint.</summary>
[WebMcpTool("delete_glossary_term", "Delete glossary term.")]
[TypeOption(typeof(CatalogEndpoints), "DeleteGlossaryTerm")]
public class DeleteGlossaryTermOption : CatalogEndpointBase<DeleteGlossaryTermEndpoint>
{
}
