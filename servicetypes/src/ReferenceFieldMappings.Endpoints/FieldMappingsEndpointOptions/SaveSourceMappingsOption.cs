using Fdw.Collections.Attributes;

namespace ReferenceFieldMappings.Endpoints.FieldMappingsEndpointOptions;

/// <summary>The SaveSourceMappings endpoint.</summary>
[TypeOption(typeof(FieldMappingsEndpoints), "SaveSourceMappings")]
public class SaveSourceMappingsOption : FieldMappingsEndpointBase<SaveSourceMappingsEndpoint>
{
}
