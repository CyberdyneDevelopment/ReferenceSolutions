using Fdw.Collections.Attributes;

namespace ReferenceUsers.Endpoints.UserPreferenceEndpointOptions;

/// <summary>The UpdateUserPreferences endpoint.</summary>
[TypeOption(typeof(UserPreferenceEndpoints), "UpdateUserPreferences")]
public class UpdateUserPreferencesOption : UserPreferenceEndpointBase<UpdateUserPreferencesEndpoint>
{
}
