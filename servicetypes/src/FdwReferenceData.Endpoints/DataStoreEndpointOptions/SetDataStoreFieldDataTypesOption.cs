using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The SetDataStoreFieldDataTypes endpoint.</summary>
[WebMcpTool("set_data_store_field_data_types", "Set the portable data type of one or more data store fields.")]
[TypeOption(typeof(DataStoreEndpoints), "SetDataStoreFieldDataTypes")]
public class SetDataStoreFieldDataTypesOption : DataStoreEndpointBase<SetDataStoreFieldDataTypesEndpoint>
{
}
