using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceUsers.Endpoints.UserRoleEndpointOptions;

/// <summary>
/// The endpoints over the user-role resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.EndpointGroups), "UserRoleEndpoints")]
[TypeCollection(typeof(UserRoleEndpointBase), typeof(IEndpointTypeOption), typeof(UserRoleEndpoints))]
public partial class UserRoleEndpoints : EndpointTypeCollectionBase<UserRoleEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
