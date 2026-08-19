using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The endpoints over the calculation-entity resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "CalculationEntityEndpoints")]
[TypeCollection(typeof(CalculationEntityEndpointBase), typeof(IEndpointTypeOption), typeof(CalculationEntityEndpoints))]
public partial class CalculationEntityEndpoints : EndpointTypeCollectionBase<CalculationEntityEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
