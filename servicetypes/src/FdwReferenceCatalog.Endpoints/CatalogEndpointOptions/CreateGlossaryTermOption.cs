using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The CreateGlossaryTerm endpoint.</summary>
[WebMcpTool("create_glossary_term", "Create glossary term.")]
[TypeOption(typeof(CatalogEndpoints), "CreateGlossaryTerm")]
public class CreateGlossaryTermOption : CatalogEndpointBase<CreateGlossaryTermEndpoint>
{
}
