using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceFieldMappings.Endpoints.FieldMappingsEndpointOptions;

/// <summary>The SaveSourceMappings endpoint.</summary>
[WebMcpTool("save_source_mappings", "Save source mappings.")]
[TypeOption(typeof(FieldMappingsEndpoints), "SaveSourceMappings")]
public class SaveSourceMappingsOption : FieldMappingsEndpointBase<SaveSourceMappingsEndpoint>
{
}
