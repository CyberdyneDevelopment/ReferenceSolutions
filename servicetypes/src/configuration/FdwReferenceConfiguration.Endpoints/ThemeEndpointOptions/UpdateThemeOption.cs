using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConfiguration.Endpoints.ThemeEndpointOptions;

/// <summary>The UpdateTheme endpoint.</summary>
[WebMcpTool("update_theme", "Update a theme")]
[TypeOption(typeof(ThemeEndpoints), "UpdateTheme")]
public class UpdateThemeOption : ThemeEndpointBase<UpdateThemeEndpoint>
{
}
