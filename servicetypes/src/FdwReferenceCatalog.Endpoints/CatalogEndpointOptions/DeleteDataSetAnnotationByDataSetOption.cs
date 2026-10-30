using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The DeleteDataSetAnnotationByDataSet endpoint.</summary>
[WebMcpTool("delete_data_set_annotation_by_data_set", "Delete the annotation of a data set, by the data set's name.")]
[TypeOption(typeof(CatalogEndpoints), "DeleteDataSetAnnotationByDataSet")]
public class DeleteDataSetAnnotationByDataSetOption : CatalogEndpointBase<DeleteDataSetAnnotationByDataSetEndpoint>
{
}
