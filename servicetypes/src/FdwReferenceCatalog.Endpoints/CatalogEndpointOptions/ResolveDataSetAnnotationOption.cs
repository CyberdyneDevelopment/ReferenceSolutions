using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The ResolveDataSetAnnotation endpoint.</summary>
[WebMcpTool("resolve_data_set_annotation", "Resolve data set annotation.")]
[TypeOption(typeof(CatalogEndpoints), "ResolveDataSetAnnotation")]
public class ResolveDataSetAnnotationOption : CatalogEndpointBase<ResolveDataSetAnnotationEndpoint>
{
}
