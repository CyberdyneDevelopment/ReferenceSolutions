using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataStores.Endpoints.DataStoreContainerEndpointOptions;

/// <summary>The GetContainerById endpoint.</summary>
[WebMcpTool("get_container_by_id", "Get container by ID.", ReadOnly = true)]
[TypeOption(typeof(DataStoreContainerEndpoints), "GetContainerById")]
public class GetContainerByIdOption : DataStoreContainerEndpointBase<GetContainerByIdEndpoint>
{
}
