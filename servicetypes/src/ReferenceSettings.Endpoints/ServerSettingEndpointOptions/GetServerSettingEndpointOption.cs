using Fdw.Collections.Attributes;

namespace ReferenceSettings.Endpoints.ServerSettingEndpointOptions;

/// <summary>The GetServerSetting endpoint.</summary>
[TypeOption(typeof(ServerSettingEndpoints), "GetServerSetting")]
public class GetServerSettingEndpointOption : ServerSettingEndpointBase<GetServerSettingEndpoint>
{
}
