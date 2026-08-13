using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceRoles.Endpoints.RoleEndpointOptions;

/// <summary>
/// The endpoints over the role resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(RoleEndpointBase), typeof(IEndpointTypeOption), typeof(RoleEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "RoleEndpoints")]
public partial class RoleEndpoints : EndpointTypeCollectionBase<RoleEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
