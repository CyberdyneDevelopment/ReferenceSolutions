using Fdw.Collections.Attributes;

namespace ReferenceUsers.Endpoints.UserEndpointOptions;

/// <summary>The ListUsers endpoint.</summary>
[TypeOption(typeof(UserEndpoints), "ListUsers")]
public class ListUsersOption : UserEndpointBase<ListUsersEndpoint>
{
}
