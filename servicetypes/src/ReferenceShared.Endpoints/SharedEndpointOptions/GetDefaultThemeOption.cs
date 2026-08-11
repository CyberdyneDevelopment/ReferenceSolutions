using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The GetDefaultTheme endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "GetDefaultTheme")]
public class GetDefaultThemeOption : SharedEndpointBase<GetDefaultThemeEndpoint>
{
}
