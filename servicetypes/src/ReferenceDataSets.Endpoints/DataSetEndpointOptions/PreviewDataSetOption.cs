using Fdw.Collections.Attributes;

namespace ReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The PreviewDataSet endpoint.</summary>
[TypeOption(typeof(DataSetEndpoints), "PreviewDataSet")]
public class PreviewDataSetOption : DataSetEndpointBase<PreviewDataSetEndpoint>
{
}
