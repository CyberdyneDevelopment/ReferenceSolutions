using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceTenants.Endpoints.TenantsEndpointOptions;

/// <summary>The endpoints over the tenants surface.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(TenantsEndpointBase), typeof(IEndpointTypeOption), typeof(TenantsEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "TenantsEndpoints")]
public partial class TenantsEndpoints : EndpointTypeCollectionBase<TenantsEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
