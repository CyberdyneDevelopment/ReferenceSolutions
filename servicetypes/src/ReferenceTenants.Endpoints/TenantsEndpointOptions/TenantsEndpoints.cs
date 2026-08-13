using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceTenants.Endpoints.TenantsEndpointOptions;

/// <summary>The endpoints over the tenants surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.EndpointGroups), "TenantsEndpoints")]
[TypeCollection(typeof(TenantsEndpointBase), typeof(IEndpointTypeOption), typeof(TenantsEndpoints))]
public partial class TenantsEndpoints : EndpointTypeCollectionBase<TenantsEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
