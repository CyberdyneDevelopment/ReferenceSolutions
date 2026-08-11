using Fdw.Collections.Attributes;

namespace ReferenceDataSets.Endpoints.DataSetSourceEndpointOptions;

/// <summary>The GetDataSetSources endpoint.</summary>
[TypeOption(typeof(DataSetSourceEndpoints), "GetDataSetSources")]
public class GetDataSetSourcesOption : DataSetSourceEndpointBase<GetDataSetSourcesEndpoint>
{
}
