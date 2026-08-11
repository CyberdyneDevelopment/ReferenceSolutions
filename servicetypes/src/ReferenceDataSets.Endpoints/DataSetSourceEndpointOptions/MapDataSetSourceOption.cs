using Fdw.Collections.Attributes;

namespace ReferenceDataSets.Endpoints.DataSetSourceEndpointOptions;

/// <summary>The MapDataSetSource endpoint.</summary>
[TypeOption(typeof(DataSetSourceEndpoints), "MapDataSetSource")]
public class MapDataSetSourceOption : DataSetSourceEndpointBase<MapDataSetSourceEndpoint>
{
}
