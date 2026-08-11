using Fdw.Collections.Attributes;

namespace ReferenceUsers.Endpoints.UserEndpointOptions;

/// <summary>The CreateUser endpoint.</summary>
[TypeOption(typeof(UserEndpoints), "CreateUser")]
public class CreateUserOption : UserEndpointBase<CreateUserEndpoint>
{
}
