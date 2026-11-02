using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserPreferenceEndpointOptions;

/// <summary>The UpdateUserPreferences endpoint.</summary>
[WebMcpTool("auth_update_user_preferences", "Update user preferences.")]
[TypeOption(typeof(UserPreferenceEndpoints), "UpdateUserPreferences")]
public class UpdateUserPreferencesOption : UserPreferenceEndpointBase<UpdateUserPreferencesEndpoint>
{
}
