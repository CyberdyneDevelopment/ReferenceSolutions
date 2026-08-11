using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceUsers.Endpoints.UserEndpointOptions;

/// <summary>
/// The endpoints over the user resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(UserEndpointBase), typeof(IEndpointTypeOption), typeof(UserEndpoints))]
public partial class UserEndpoints : EndpointTypeCollectionBase<UserEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
