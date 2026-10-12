using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The GetDataSetAnnotation endpoint.</summary>
[WebMcpTool("get_data_set_annotation", "Get data set annotation.", ReadOnly = true)]
[TypeOption(typeof(CatalogEndpoints), "GetDataSetAnnotation")]
public class GetDataSetAnnotationOption : CatalogEndpointBase<GetDataSetAnnotationEndpoint>
{
}
