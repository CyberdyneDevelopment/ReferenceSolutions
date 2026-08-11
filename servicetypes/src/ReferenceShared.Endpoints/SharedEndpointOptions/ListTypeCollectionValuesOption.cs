using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The ListTypeCollectionValues endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "ListTypeCollectionValues")]
public class ListTypeCollectionValuesOption : SharedEndpointBase<ListTypeCollectionValuesEndpoint>
{
}
