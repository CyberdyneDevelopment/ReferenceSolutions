using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The ListGlossaryTerms endpoint.</summary>
[WebMcpTool("list_glossary_terms", "List glossary terms.", ReadOnly = true)]
[TypeOption(typeof(CatalogEndpoints), "ListGlossaryTerms")]
public class ListGlossaryTermsOption : CatalogEndpointBase<ListGlossaryTermsEndpoint>
{
}
