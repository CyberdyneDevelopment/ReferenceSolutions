using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserPreferenceEndpointOptions;

/// <summary>The GetUserPreferences endpoint.</summary>
[WebMcpTool("get_user_preferences", "Get user preferences.", ReadOnly = true)]
[TypeOption(typeof(UserPreferenceEndpoints), "GetUserPreferences")]
public class GetUserPreferencesOption : UserPreferenceEndpointBase<GetUserPreferencesEndpoint>
{
}
