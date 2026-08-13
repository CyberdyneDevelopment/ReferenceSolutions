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
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "UserRoleEndpoints")]
[TypeCollection(typeof(UserRoleEndpointBase), typeof(IEndpointTypeOption), typeof(UserRoleEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "UserRoleEndpoints")]
public partial class UserRoleEndpoints : EndpointTypeCollectionBase<UserRoleEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
