using Fdw.Collections.Attributes;

namespace ReferenceSchema.Endpoints.DataPreviewEndpointOptions;

/// <summary>The DataPreview endpoint.</summary>
[TypeOption(typeof(DataPreviewEndpoints), "DataPreview")]
public class DataPreviewOption : DataPreviewEndpointBase<DataPreviewEndpoint>
{
}
