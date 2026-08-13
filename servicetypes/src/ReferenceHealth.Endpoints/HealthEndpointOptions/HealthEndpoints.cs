using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceHealth.Endpoints.HealthEndpointOptions;

/// <summary>The endpoints over the health surface.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "HealthEndpoints")]
[TypeCollection(typeof(HealthEndpointBase), typeof(IEndpointTypeOption), typeof(HealthEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "HealthEndpoints")]
public partial class HealthEndpoints : EndpointTypeCollectionBase<HealthEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
