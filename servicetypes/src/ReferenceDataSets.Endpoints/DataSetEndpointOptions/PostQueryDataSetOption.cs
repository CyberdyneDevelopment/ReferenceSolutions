using Fdw.Collections.Attributes;

namespace ReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The PostQueryDataSet endpoint.</summary>
[TypeOption(typeof(DataSetEndpoints), "PostQueryDataSet")]
public class PostQueryDataSetOption : DataSetEndpointBase<PostQueryDataSetEndpoint>
{
}
