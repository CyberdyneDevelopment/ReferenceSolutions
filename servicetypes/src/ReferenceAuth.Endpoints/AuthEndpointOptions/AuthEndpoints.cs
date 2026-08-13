using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The endpoints over the auth resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.EndpointGroups), "AuthEndpoints")]
[TypeCollection(typeof(AuthEndpointBase), typeof(IEndpointTypeOption), typeof(AuthEndpoints))]
public partial class AuthEndpoints : EndpointTypeCollectionBase<AuthEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
