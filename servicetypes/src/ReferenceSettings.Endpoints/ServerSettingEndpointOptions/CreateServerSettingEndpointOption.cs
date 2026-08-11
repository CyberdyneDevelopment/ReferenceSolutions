using Fdw.Collections.Attributes;

namespace ReferenceSettings.Endpoints.ServerSettingEndpointOptions;

/// <summary>The CreateServerSetting endpoint.</summary>
[TypeOption(typeof(ServerSettingEndpoints), "CreateServerSetting")]
public class CreateServerSettingEndpointOption : ServerSettingEndpointBase<CreateServerSettingEndpoint>
{
}
