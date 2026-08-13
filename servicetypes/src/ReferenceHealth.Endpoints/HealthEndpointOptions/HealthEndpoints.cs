using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceHealth.Endpoints.HealthEndpointOptions;

/// <summary>The endpoints over the health surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.EndpointGroups), "HealthEndpoints")]
[TypeCollection(typeof(HealthEndpointBase), typeof(IEndpointTypeOption), typeof(HealthEndpoints))]
public partial class HealthEndpoints : EndpointTypeCollectionBase<HealthEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
