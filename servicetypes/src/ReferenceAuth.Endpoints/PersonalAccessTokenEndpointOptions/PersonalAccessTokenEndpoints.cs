using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceAuth.Endpoints.PersonalAccessTokenEndpointOptions;

/// <summary>The endpoints over the personal-access-token resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "PersonalAccessTokenEndpoints")]
[TypeCollection(typeof(PersonalAccessTokenEndpointBase), typeof(IEndpointTypeOption), typeof(PersonalAccessTokenEndpoints))]
public partial class PersonalAccessTokenEndpoints : EndpointTypeCollectionBase<PersonalAccessTokenEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
