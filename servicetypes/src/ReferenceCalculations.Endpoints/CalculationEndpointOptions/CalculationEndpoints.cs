using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceCalculations.Endpoints.CalculationEndpointOptions;

/// <summary>The endpoints over the calculation resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.EndpointGroups), "CalculationEndpoints")]
[TypeCollection(typeof(CalculationEndpointBase), typeof(IEndpointTypeOption), typeof(CalculationEndpoints))]
public partial class CalculationEndpoints : EndpointTypeCollectionBase<CalculationEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
