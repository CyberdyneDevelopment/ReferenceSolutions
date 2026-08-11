using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The GetDataSetAnnotation endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "GetDataSetAnnotation")]
public class GetDataSetAnnotationOption : SharedEndpointBase<GetDataSetAnnotationEndpoint>
{
}
