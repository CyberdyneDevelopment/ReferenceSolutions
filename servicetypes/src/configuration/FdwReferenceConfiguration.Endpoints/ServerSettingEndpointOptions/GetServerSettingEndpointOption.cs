using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.ServerSettingEndpointOptions;

/// <summary>The GetServerSetting endpoint.</summary>
[WebMcpTool("get_server_setting", "Get server setting.", ReadOnly = true)]
[TypeOption(typeof(ServerSettingEndpoints), "GetServerSetting")]
public class GetServerSettingEndpointOption : ServerSettingEndpointBase<GetServerSettingEndpoint>
{
}
