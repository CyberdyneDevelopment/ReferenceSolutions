using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The DeleteDataSetAnnotation endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "DeleteDataSetAnnotation")]
public class DeleteDataSetAnnotationOption : SharedEndpointBase<DeleteDataSetAnnotationEndpoint>
{
}
