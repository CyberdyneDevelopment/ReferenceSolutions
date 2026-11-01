using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The ClassifyDataSetFields endpoint.</summary>
[WebMcpTool("classify_data_set_fields", "Set the data classification of one or more data set fields.")]
[TypeOption(typeof(DataSetEndpoints), "ClassifyDataSetFields")]
public class ClassifyDataSetFieldsOption : DataSetEndpointBase<ClassifyDataSetFieldsEndpoint>
{
}
