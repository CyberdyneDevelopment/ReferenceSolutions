using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The DeleteTheme endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "DeleteTheme")]
public class DeleteThemeOption : SharedEndpointBase<DeleteThemeEndpoint>
{
}
