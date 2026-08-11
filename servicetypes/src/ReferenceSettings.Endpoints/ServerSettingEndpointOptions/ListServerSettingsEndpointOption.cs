using Fdw.Collections.Attributes;

namespace ReferenceSettings.Endpoints.ServerSettingEndpointOptions;

/// <summary>The ListServerSettings endpoint.</summary>
[TypeOption(typeof(ServerSettingEndpoints), "ListServerSettings")]
public class ListServerSettingsEndpointOption : ServerSettingEndpointBase<ListServerSettingsEndpoint>
{
}
