using Fdw.Collections.Attributes;

namespace ReferenceSettings.Endpoints.ServerSettingEndpointOptions;

/// <summary>The UpdateServerSetting endpoint.</summary>
[TypeOption(typeof(ServerSettingEndpoints), "UpdateServerSetting")]
public class UpdateServerSettingEndpointOption : ServerSettingEndpointBase<UpdateServerSettingEndpoint>
{
}
