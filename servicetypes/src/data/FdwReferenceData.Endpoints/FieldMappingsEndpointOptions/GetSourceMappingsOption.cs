using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceFieldMappings.Endpoints.FieldMappingsEndpointOptions;

/// <summary>The GetSourceMappings endpoint.</summary>
[WebMcpTool("get_source_mappings", "Get source mappings.", ReadOnly = true)]
[TypeOption(typeof(FieldMappingsEndpoints), "GetSourceMappings")]
public class GetSourceMappingsOption : FieldMappingsEndpointBase<GetSourceMappingsEndpoint>
{
}
