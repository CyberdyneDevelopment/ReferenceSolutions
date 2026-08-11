using Fdw.Collections.Attributes;

namespace ReferenceUsers.Endpoints.UserEndpointOptions;

/// <summary>The UpdateUser endpoint.</summary>
[TypeOption(typeof(UserEndpoints), "UpdateUser")]
public class UpdateUserOption : UserEndpointBase<UpdateUserEndpoint>
{
}
