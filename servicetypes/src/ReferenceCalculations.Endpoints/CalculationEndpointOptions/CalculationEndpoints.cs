using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceCalculations.Endpoints.CalculationEndpointOptions;

/// <summary>The endpoints over the calculation resource.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "CalculationEndpoints")]
[TypeCollection(typeof(CalculationEndpointBase), typeof(IEndpointTypeOption), typeof(CalculationEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "CalculationEndpoints")]
public partial class CalculationEndpoints : EndpointTypeCollectionBase<CalculationEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
