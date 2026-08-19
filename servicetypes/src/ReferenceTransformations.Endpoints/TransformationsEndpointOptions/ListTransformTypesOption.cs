using Fdw.Collections.Attributes;

namespace ReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The ListTransformTypes endpoint.</summary>
[TypeOption(typeof(TransformationsEndpoints), "ListTransformTypes")]
public class ListTransformTypesOption : TransformationsEndpointBase<ListTransformTypesEndpoint>
{
}
