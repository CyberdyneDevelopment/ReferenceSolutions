using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConfiguration.Endpoints.ThemeEndpointOptions;

/// <summary>The DeleteTheme endpoint.</summary>
[WebMcpTool("delete_theme", "Delete a theme")]
[TypeOption(typeof(ThemeEndpoints), "DeleteTheme")]
public class DeleteThemeOption : ThemeEndpointBase<DeleteThemeEndpoint>
{
}
