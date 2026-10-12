using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConfiguration.Endpoints.ThemeEndpointOptions;

/// <summary>The GetTheme endpoint.</summary>
[WebMcpTool("get_theme", "Get theme by name", ReadOnly = true)]
[TypeOption(typeof(ThemeEndpoints), "GetTheme")]
public class GetThemeOption : ThemeEndpointBase<GetThemeEndpoint>
{
}
