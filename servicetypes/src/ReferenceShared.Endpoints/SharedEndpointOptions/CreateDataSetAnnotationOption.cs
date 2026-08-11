using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The CreateDataSetAnnotation endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "CreateDataSetAnnotation")]
public class CreateDataSetAnnotationOption : SharedEndpointBase<CreateDataSetAnnotationEndpoint>
{
}
