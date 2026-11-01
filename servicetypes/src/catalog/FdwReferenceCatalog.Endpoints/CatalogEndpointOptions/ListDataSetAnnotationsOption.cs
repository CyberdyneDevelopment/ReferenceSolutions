using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The ListDataSetAnnotations endpoint.</summary>
[WebMcpTool("list_data_set_annotations", "List data set annotations.", ReadOnly = true)]
[TypeOption(typeof(CatalogEndpoints), "ListDataSetAnnotations")]
public class ListDataSetAnnotationsOption : CatalogEndpointBase<ListDataSetAnnotationsEndpoint>
{
}
