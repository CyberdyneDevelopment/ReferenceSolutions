using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceUsers.Endpoints.UserPreferenceEndpointOptions;

/// <summary>
/// The endpoints over the user-preference resource.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "UserPreferenceEndpoints")]
[TypeCollection(typeof(UserPreferenceEndpointBase), typeof(IEndpointTypeOption), typeof(UserPreferenceEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "UserPreferenceEndpoints")]
public partial class UserPreferenceEndpoints : EndpointTypeCollectionBase<UserPreferenceEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
