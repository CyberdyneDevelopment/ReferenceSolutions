using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.ServerSettingEndpointOptions;

/// <summary>The CreateServerSetting endpoint.</summary>
[WebMcpTool("create_server_setting", "Create server setting.")]
[TypeOption(typeof(ServerSettingEndpoints), "CreateServerSetting")]
public class CreateServerSettingEndpointOption : ServerSettingEndpointBase<CreateServerSettingEndpoint>
{
}
