using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The ListDataSetAnnotations endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "ListDataSetAnnotations")]
public class ListDataSetAnnotationsOption : SharedEndpointBase<ListDataSetAnnotationsEndpoint>
{
}
