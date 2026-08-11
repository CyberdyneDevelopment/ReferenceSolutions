using Fdw.Collections.Attributes;

namespace ReferenceUsers.Endpoints.UserRoleEndpointOptions;

/// <summary>The RevokeUserRole endpoint.</summary>
[TypeOption(typeof(UserRoleEndpoints), "RevokeUserRole")]
public class RevokeUserRoleOption : UserRoleEndpointBase<RevokeUserRoleEndpoint>
{
}
