using Fdw.Collections.Attributes;

namespace ReferenceFieldMappings.Endpoints.FieldMappingsEndpointOptions;

/// <summary>The ValidateMappings endpoint.</summary>
[TypeOption(typeof(FieldMappingsEndpoints), "ValidateMappings")]
public class ValidateMappingsOption : FieldMappingsEndpointBase<ValidateMappingsEndpoint>
{
}
