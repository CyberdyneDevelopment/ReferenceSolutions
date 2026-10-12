using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The GetGlossaryTerm endpoint.</summary>
[WebMcpTool("get_glossary_term", "Get glossary term.", ReadOnly = true)]
[TypeOption(typeof(CatalogEndpoints), "GetGlossaryTerm")]
public class GetGlossaryTermOption : CatalogEndpointBase<GetGlossaryTermEndpoint>
{
}
