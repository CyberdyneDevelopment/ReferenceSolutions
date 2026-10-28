using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The ListGlossaryTermVersions endpoint (FDW-2331).</summary>
[WebMcpTool("list_glossary_term_versions", "List the versions of a glossary term at the places the caller may read, nearest first.", ReadOnly = true)]
[TypeOption(typeof(CatalogEndpoints), "ListGlossaryTermVersions")]
public class ListGlossaryTermVersionsOption : CatalogEndpointBase<ListGlossaryTermVersionsEndpoint>
{
}
