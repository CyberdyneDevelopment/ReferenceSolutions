using Fdw.Collections.Attributes;

namespace ReferenceDataSets.Endpoints.DataSetSourceEndpointOptions;

/// <summary>The SearchDataSetSource endpoint.</summary>
[TypeOption(typeof(DataSetSourceEndpoints), "SearchDataSetSource")]
public class SearchDataSetSourceOption : DataSetSourceEndpointBase<SearchDataSetSourceEndpoint>
{
}
