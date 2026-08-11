using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The ListThemes endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "ListThemes")]
public class ListThemesOption : SharedEndpointBase<ListThemesEndpoint>
{
}
