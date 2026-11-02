using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The SetDataStoreFieldDatumTypes endpoint.</summary>
[WebMcpTool("set_data_store_field_datum_types", "Set the datum type of one or more data store fields.")]
[TypeOption(typeof(DataStoreEndpoints), "SetDataStoreFieldDatumTypes")]
public class SetDataStoreFieldDatumTypesOption : DataStoreEndpointBase<SetDataStoreFieldDatumTypesEndpoint>
{
}
