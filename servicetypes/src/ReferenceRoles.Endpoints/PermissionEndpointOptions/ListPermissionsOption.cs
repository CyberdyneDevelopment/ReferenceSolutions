using Fdw.Collections.Attributes;

namespace ReferenceRoles.Endpoints.PermissionEndpointOptions;

/// <summary>The ListPermissions endpoint.</summary>
[TypeOption(typeof(PermissionEndpoints), "ListPermissions")]
public class ListPermissionsOption : PermissionEndpointBase<ListPermissionsEndpoint>
{
}
