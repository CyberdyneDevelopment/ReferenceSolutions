using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConfiguration.Endpoints.ThemeEndpointOptions;

/// <summary>The GetDefaultTheme endpoint.</summary>
[WebMcpTool("get_default_theme", "Get default theme", ReadOnly = true)]
[TypeOption(typeof(ThemeEndpoints), "GetDefaultTheme")]
public class GetDefaultThemeOption : ThemeEndpointBase<GetDefaultThemeEndpoint>
{
}
