using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The endpoints over the calculation-entity resource.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "CalculationEntityEndpoints")]
[TypeCollection(typeof(CalculationEntityEndpointBase), typeof(IEndpointTypeOption), typeof(CalculationEntityEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "CalculationEntityEndpoints")]
public partial class CalculationEntityEndpoints : EndpointTypeCollectionBase<CalculationEntityEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
