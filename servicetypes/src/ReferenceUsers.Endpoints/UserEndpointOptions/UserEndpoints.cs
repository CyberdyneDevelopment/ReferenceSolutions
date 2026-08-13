using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceUsers.Endpoints.UserEndpointOptions;

/// <summary>
/// The endpoints over the user resource.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "UserEndpoints")]
[TypeCollection(typeof(UserEndpointBase), typeof(IEndpointTypeOption), typeof(UserEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "UserEndpoints")]
public partial class UserEndpoints : EndpointTypeCollectionBase<UserEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
