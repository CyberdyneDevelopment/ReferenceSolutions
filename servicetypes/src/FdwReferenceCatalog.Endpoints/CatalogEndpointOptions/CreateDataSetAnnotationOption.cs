using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The CreateDataSetAnnotation endpoint.</summary>
[WebMcpTool("create_data_set_annotation", "Create data set annotation.")]
[TypeOption(typeof(CatalogEndpoints), "CreateDataSetAnnotation")]
public class CreateDataSetAnnotationOption : CatalogEndpointBase<CreateDataSetAnnotationEndpoint>
{
}
