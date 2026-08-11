using Fdw.Collections.Attributes;

namespace ReferenceRoles.Endpoints.RoleEndpointOptions;

/// <summary>The DeleteRole endpoint.</summary>
[TypeOption(typeof(RoleEndpoints), "DeleteRole")]
public class DeleteRoleOption : RoleEndpointBase<DeleteRoleEndpoint>
{
}
