using Fdw.Collections.Attributes;

namespace ReferenceUsers.Endpoints.UserRoleEndpointOptions;

/// <summary>The GetUserRoles endpoint.</summary>
[TypeOption(typeof(UserRoleEndpoints), "GetUserRoles")]
public class GetUserRolesOption : UserRoleEndpointBase<GetUserRolesEndpoint>
{
}
