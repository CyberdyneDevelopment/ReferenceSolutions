using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The UpdateTheme endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "UpdateTheme")]
public class UpdateThemeOption : SharedEndpointBase<UpdateThemeEndpoint>
{
}
