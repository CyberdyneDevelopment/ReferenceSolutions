using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The endpoints over the tenants surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "TenantsEndpoints")]
[TypeCollection(typeof(TenantsEndpointBase), typeof(IEndpointTypeOption), typeof(TenantsEndpoints))]
public partial class TenantsEndpoints : EndpointTypeCollectionBase<TenantsEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
