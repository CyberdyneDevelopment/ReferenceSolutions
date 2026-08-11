using Fdw.Collections.Attributes;

namespace ReferenceRoles.Endpoints.RoleEndpointOptions;

/// <summary>The GetRole endpoint.</summary>
[TypeOption(typeof(RoleEndpoints), "GetRole")]
public class GetRoleOption : RoleEndpointBase<GetRoleEndpoint>
{
}
