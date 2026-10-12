using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConfiguration.Endpoints.ThemeEndpointOptions;

/// <summary>The CreateTheme endpoint.</summary>
[WebMcpTool("create_theme", "Create a new theme")]
[TypeOption(typeof(ThemeEndpoints), "CreateTheme")]
public class CreateThemeOption : ThemeEndpointBase<CreateThemeEndpoint>
{
}
