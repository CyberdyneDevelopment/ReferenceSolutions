using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.ServerSettingEndpointOptions;

/// <summary>The ListServerSettings endpoint.</summary>
[WebMcpTool("list_server_settings", "List server settings.", ReadOnly = true)]
[TypeOption(typeof(ServerSettingEndpoints), "ListServerSettings")]
public class ListServerSettingsEndpointOption : ServerSettingEndpointBase<ListServerSettingsEndpoint>
{
}
