using Fdw.Collections.Attributes;

namespace ReferenceFieldMappings.Endpoints.FieldMappingsEndpointOptions;

/// <summary>The GetDataSetMappings endpoint.</summary>
[TypeOption(typeof(FieldMappingsEndpoints), "GetDataSetMappings")]
public class GetDataSetMappingsOption : FieldMappingsEndpointBase<GetDataSetMappingsEndpoint>
{
}
