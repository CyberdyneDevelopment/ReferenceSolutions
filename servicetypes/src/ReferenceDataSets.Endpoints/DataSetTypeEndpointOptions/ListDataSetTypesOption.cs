using Fdw.Collections.Attributes;

namespace ReferenceDataSets.Endpoints.DataSetTypeEndpointOptions;

/// <summary>The ListDataSetTypes endpoint.</summary>
[TypeOption(typeof(DataSetTypeEndpoints), "ListDataSetTypes")]
public class ListDataSetTypesOption : DataSetTypeEndpointBase<ListDataSetTypesEndpoint>
{
}
