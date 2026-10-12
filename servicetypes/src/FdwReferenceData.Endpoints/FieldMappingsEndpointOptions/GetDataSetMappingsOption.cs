using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceFieldMappings.Endpoints.FieldMappingsEndpointOptions;

/// <summary>The GetDataSetMappings endpoint.</summary>
[WebMcpTool("get_data_set_mappings", "Get data set mappings.", ReadOnly = true)]
[TypeOption(typeof(FieldMappingsEndpoints), "GetDataSetMappings")]
public class GetDataSetMappingsOption : FieldMappingsEndpointBase<GetDataSetMappingsEndpoint>
{
}
