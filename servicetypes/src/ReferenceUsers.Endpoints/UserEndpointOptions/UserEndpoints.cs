using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceUsers.Endpoints.UserEndpointOptions;

/// <summary>
/// The endpoints over the user resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "UserEndpoints")]
[TypeCollection(typeof(UserEndpointBase), typeof(IEndpointTypeOption), typeof(UserEndpoints))]
public partial class UserEndpoints : EndpointTypeCollectionBase<UserEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
