using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceAuth.Endpoints.PersonalAccessTokenEndpointOptions;

/// <summary>The endpoints over the personal-access-token resource.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PersonalAccessTokenEndpointBase), typeof(IEndpointTypeOption), typeof(PersonalAccessTokenEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "PersonalAccessTokenEndpoints")]
public partial class PersonalAccessTokenEndpoints : EndpointTypeCollectionBase<PersonalAccessTokenEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
