using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSettings.Endpoints.ServerSettingEndpointOptions;

/// <summary>The UpdateServerSetting endpoint.</summary>
[WebMcpTool("update_server_setting", "Update server setting.")]
[TypeOption(typeof(ServerSettingEndpoints), "UpdateServerSetting")]
public class UpdateServerSettingEndpointOption : ServerSettingEndpointBase<UpdateServerSettingEndpoint>
{
}
