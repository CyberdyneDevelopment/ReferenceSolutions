using Fdw.Collections.Attributes;

namespace ReferenceRoles.Endpoints.PermissionEndpointOptions;

/// <summary>The ListPermissionsGrouped endpoint.</summary>
[TypeOption(typeof(PermissionEndpoints), "ListPermissionsGrouped")]
public class ListPermissionsGroupedOption : PermissionEndpointBase<ListPermissionsGroupedEndpoint>
{
}
