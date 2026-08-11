using Fdw.Collections.Attributes;

namespace ReferenceUsers.Endpoints.UserPreferenceEndpointOptions;

/// <summary>The GetUserPreferences endpoint.</summary>
[TypeOption(typeof(UserPreferenceEndpoints), "GetUserPreferences")]
public class GetUserPreferencesOption : UserPreferenceEndpointBase<GetUserPreferencesEndpoint>
{
}
