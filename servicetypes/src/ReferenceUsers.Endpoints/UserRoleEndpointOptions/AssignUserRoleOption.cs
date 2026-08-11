using Fdw.Collections.Attributes;

namespace ReferenceUsers.Endpoints.UserRoleEndpointOptions;

/// <summary>The AssignUserRole endpoint.</summary>
[TypeOption(typeof(UserRoleEndpoints), "AssignUserRole")]
public class AssignUserRoleOption : UserRoleEndpointBase<AssignUserRoleEndpoint>
{
}
