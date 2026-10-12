using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The DeleteDataSetAnnotation endpoint.</summary>
[WebMcpTool("delete_data_set_annotation", "Delete data set annotation.")]
[TypeOption(typeof(CatalogEndpoints), "DeleteDataSetAnnotation")]
public class DeleteDataSetAnnotationOption : CatalogEndpointBase<DeleteDataSetAnnotationEndpoint>
{
}
