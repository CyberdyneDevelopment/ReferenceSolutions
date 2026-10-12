using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The SetDataSetFieldsFilterable endpoint.</summary>
[WebMcpTool("set_data_set_fields_filterable", "Choose whether API callers may filter on one or more data set fields.")]
[TypeOption(typeof(DataSetEndpoints), "SetDataSetFieldsFilterable")]
public class SetDataSetFieldsFilterableOption : DataSetEndpointBase<SetDataSetFieldsFilterableEndpoint>
{
}
