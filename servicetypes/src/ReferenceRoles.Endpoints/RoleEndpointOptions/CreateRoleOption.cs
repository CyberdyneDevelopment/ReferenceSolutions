using Fdw.Collections.Attributes;

namespace ReferenceRoles.Endpoints.RoleEndpointOptions;

/// <summary>The CreateRole endpoint.</summary>
[TypeOption(typeof(RoleEndpoints), "CreateRole")]
public class CreateRoleOption : RoleEndpointBase<CreateRoleEndpoint>
{
}
