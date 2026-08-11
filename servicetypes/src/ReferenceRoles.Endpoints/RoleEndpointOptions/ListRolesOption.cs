using Fdw.Collections.Attributes;

namespace ReferenceRoles.Endpoints.RoleEndpointOptions;

/// <summary>The ListRoles endpoint.</summary>
[TypeOption(typeof(RoleEndpoints), "ListRoles")]
public class ListRolesOption : RoleEndpointBase<ListRolesEndpoint>
{
}
