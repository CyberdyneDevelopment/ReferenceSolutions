using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConfiguration.Endpoints.ThemeEndpointOptions;

/// <summary>The ListThemes endpoint.</summary>
[WebMcpTool("list_themes", "List available themes", ReadOnly = true)]
[TypeOption(typeof(ThemeEndpoints), "ListThemes")]
public class ListThemesOption : ThemeEndpointBase<ListThemesEndpoint>
{
}
