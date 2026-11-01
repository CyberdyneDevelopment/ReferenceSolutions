using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSchema.Endpoints.DataPreviewEndpointOptions;

/// <summary>The DataPreview endpoint.</summary>
[WebMcpTool("data_preview", "Data preview.")]
[TypeOption(typeof(DataPreviewEndpoints), "DataPreview")]
public class DataPreviewOption : DataPreviewEndpointBase<DataPreviewEndpoint>
{
}
