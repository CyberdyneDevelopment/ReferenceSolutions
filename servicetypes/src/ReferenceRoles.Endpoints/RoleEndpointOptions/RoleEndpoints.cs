using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceRoles.Endpoints.RoleEndpointOptions;

/// <summary>
/// The endpoints over the role resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "RoleEndpoints")]
[TypeCollection(typeof(RoleEndpointBase), typeof(IEndpointTypeOption), typeof(RoleEndpoints))]
public partial class RoleEndpoints : EndpointTypeCollectionBase<RoleEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
