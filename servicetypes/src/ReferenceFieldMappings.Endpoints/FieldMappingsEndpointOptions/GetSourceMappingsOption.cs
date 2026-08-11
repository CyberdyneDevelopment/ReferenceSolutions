using Fdw.Collections.Attributes;

namespace ReferenceFieldMappings.Endpoints.FieldMappingsEndpointOptions;

/// <summary>The GetSourceMappings endpoint.</summary>
[TypeOption(typeof(FieldMappingsEndpoints), "GetSourceMappings")]
public class GetSourceMappingsOption : FieldMappingsEndpointBase<GetSourceMappingsEndpoint>
{
}
