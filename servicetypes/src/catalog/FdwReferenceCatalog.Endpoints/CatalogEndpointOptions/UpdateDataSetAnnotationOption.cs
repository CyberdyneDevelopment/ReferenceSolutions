using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The UpdateDataSetAnnotation endpoint.</summary>
[WebMcpTool("update_data_set_annotation", "Update data set annotation.")]
[TypeOption(typeof(CatalogEndpoints), "UpdateDataSetAnnotation")]
public class UpdateDataSetAnnotationOption : CatalogEndpointBase<UpdateDataSetAnnotationEndpoint>
{
}
