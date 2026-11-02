using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConfiguration.Endpoints.ThemeEndpointOptions;

/// <summary>The SetDefaultTheme endpoint.</summary>
[WebMcpTool("set_default_theme", "Set default theme")]
[TypeOption(typeof(ThemeEndpoints), "SetDefaultTheme")]
public class SetDefaultThemeOption : ThemeEndpointBase<SetDefaultThemeEndpoint>
{
}
