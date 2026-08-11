using Fdw.Collections.Attributes;

namespace ReferenceUsers.Endpoints.UserEndpointOptions;

/// <summary>The DeleteUser endpoint.</summary>
[TypeOption(typeof(UserEndpoints), "DeleteUser")]
public class DeleteUserOption : UserEndpointBase<DeleteUserEndpoint>
{
}
