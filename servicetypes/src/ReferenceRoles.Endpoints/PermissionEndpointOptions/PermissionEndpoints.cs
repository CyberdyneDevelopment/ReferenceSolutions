using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceRoles.Endpoints.PermissionEndpointOptions;

/// <summary>
/// The endpoints over the permission resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PermissionEndpointBase), typeof(IEndpointTypeOption), typeof(PermissionEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "PermissionEndpoints")]
public partial class PermissionEndpoints : EndpointTypeCollectionBase<PermissionEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
