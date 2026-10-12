using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The SetDataSetFieldDatumTypes endpoint.</summary>
[WebMcpTool("set_data_set_field_datum_types", "Set the datum type of one or more data set fields.")]
[TypeOption(typeof(DataSetEndpoints), "SetDataSetFieldDatumTypes")]
public class SetDataSetFieldDatumTypesOption : DataSetEndpointBase<SetDataSetFieldDatumTypesEndpoint>
{
}
