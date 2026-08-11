using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The SetDefaultTheme endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "SetDefaultTheme")]
public class SetDefaultThemeOption : SharedEndpointBase<SetDefaultThemeEndpoint>
{
}
