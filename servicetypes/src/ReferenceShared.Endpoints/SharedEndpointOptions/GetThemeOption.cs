using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The GetTheme endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "GetTheme")]
public class GetThemeOption : SharedEndpointBase<GetThemeEndpoint>
{
}
