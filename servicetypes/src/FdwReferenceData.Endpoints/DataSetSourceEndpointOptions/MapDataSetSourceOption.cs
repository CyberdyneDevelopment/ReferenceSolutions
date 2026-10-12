using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetSourceEndpointOptions;

/// <summary>The MapDataSetSource endpoint.</summary>
[WebMcpTool("map_data_set_source", "Map a DataSet as a source")]
[TypeOption(typeof(DataSetSourceEndpoints), "MapDataSetSource")]
public class MapDataSetSourceOption : DataSetSourceEndpointBase<MapDataSetSourceEndpoint>
{
}
