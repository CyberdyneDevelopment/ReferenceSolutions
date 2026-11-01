using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The SaveGlossaryTermVersion endpoint (FDW-2331).</summary>
[WebMcpTool("save_glossary_term_version", "Save a version of a glossary term at an organization or workspace.")]
[TypeOption(typeof(CatalogEndpoints), "SaveGlossaryTermVersion")]
public class SaveGlossaryTermVersionOption : CatalogEndpointBase<SaveGlossaryTermVersionEndpoint>
{
}
