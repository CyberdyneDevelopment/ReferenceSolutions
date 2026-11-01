using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The UpdateGlossaryTerm endpoint.</summary>
[WebMcpTool("update_glossary_term", "Update glossary term.")]
[TypeOption(typeof(CatalogEndpoints), "UpdateGlossaryTerm")]
public class UpdateGlossaryTermOption : CatalogEndpointBase<UpdateGlossaryTermEndpoint>
{
}
