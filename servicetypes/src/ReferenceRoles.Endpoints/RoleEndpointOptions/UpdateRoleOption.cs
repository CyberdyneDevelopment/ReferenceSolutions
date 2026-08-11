using Fdw.Collections.Attributes;

namespace ReferenceRoles.Endpoints.RoleEndpointOptions;

/// <summary>The UpdateRole endpoint.</summary>
[TypeOption(typeof(RoleEndpoints), "UpdateRole")]
public class UpdateRoleOption : RoleEndpointBase<UpdateRoleEndpoint>
{
}
