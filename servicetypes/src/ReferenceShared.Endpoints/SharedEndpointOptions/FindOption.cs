using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The Find endpoint.</summary>
// Why no closure class: Fdw.Web.Search.Endpoints.FindEndpoint is concrete and complete — it needs
// declaring, not specialising. Deriving an empty closure would add a second type for nothing.
[TypeOption(typeof(SharedEndpoints), "Find")]
public class FindOption : SharedEndpointBase<Fdw.Web.Search.Endpoints.FindEndpoint>
{
}
