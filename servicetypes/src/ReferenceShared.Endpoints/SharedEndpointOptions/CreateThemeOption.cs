using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The CreateTheme endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "CreateTheme")]
public class CreateThemeOption : SharedEndpointBase<CreateThemeEndpoint>
{
}
