using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The endpoints over the auth resource.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(AuthEndpointBase), typeof(IEndpointTypeOption), typeof(AuthEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "AuthEndpoints")]
public partial class AuthEndpoints : EndpointTypeCollectionBase<AuthEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
