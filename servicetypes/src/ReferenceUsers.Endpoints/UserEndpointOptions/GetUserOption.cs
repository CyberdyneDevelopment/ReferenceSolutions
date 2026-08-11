using Fdw.Collections.Attributes;

namespace ReferenceUsers.Endpoints.UserEndpointOptions;

/// <summary>The GetUser endpoint.</summary>
[TypeOption(typeof(UserEndpoints), "GetUser")]
public class GetUserOption : UserEndpointBase<GetUserEndpoint>
{
}
