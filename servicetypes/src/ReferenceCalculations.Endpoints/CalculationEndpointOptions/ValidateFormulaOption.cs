using Fdw.Collections.Attributes;

namespace ReferenceCalculations.Endpoints.CalculationEndpointOptions;

/// <summary>The ValidateFormula endpoint.</summary>
[TypeOption(typeof(CalculationEndpoints), "ValidateFormula")]
public class ValidateFormulaOption : CalculationEndpointBase<ValidateFormulaEndpoint>
{
}
