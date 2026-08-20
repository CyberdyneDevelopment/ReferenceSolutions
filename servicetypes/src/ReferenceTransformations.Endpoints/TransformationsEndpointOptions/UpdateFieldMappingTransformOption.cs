using Fdw.Collections.Attributes;

namespace ReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>Declares the update endpoint so the collection maps it.</summary>
[TypeOption(typeof(TransformationsEndpoints), "UpdateFieldMappingTransform")]
public class UpdateFieldMappingTransformOption : TransformationsEndpointBase<UpdateFieldMappingTransformEndpoint>
{
}
