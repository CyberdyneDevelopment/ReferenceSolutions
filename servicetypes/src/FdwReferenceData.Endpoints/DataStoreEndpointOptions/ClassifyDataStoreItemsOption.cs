using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The ClassifyDataStoreItems endpoint.</summary>
[WebMcpTool("classify_data_store_items", "Set the data classification of one or more data store paths, containers or fields.")]
[TypeOption(typeof(DataStoreEndpoints), "ClassifyDataStoreItems")]
public class ClassifyDataStoreItemsOption : DataStoreEndpointBase<ClassifyDataStoreItemsEndpoint>
{
}
