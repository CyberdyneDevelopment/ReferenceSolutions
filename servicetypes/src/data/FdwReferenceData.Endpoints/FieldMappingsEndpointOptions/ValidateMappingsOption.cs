using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceFieldMappings.Endpoints.FieldMappingsEndpointOptions;

/// <summary>The ValidateMappings endpoint.</summary>
[WebMcpTool("validate_mappings", "Validate mappings.")]
[TypeOption(typeof(FieldMappingsEndpoints), "ValidateMappings")]
public class ValidateMappingsOption : FieldMappingsEndpointBase<ValidateMappingsEndpoint>
{
}
