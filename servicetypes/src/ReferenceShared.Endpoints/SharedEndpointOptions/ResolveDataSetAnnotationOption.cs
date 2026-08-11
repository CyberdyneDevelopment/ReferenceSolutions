using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The ResolveDataSetAnnotation endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "ResolveDataSetAnnotation")]
public class ResolveDataSetAnnotationOption : SharedEndpointBase<ResolveDataSetAnnotationEndpoint>
{
}
