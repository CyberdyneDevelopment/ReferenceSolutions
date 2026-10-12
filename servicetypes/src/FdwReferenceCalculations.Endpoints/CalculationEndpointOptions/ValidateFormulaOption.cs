using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEndpointOptions;

/// <summary>The ValidateFormula endpoint.</summary>
[WebMcpTool("validate_formula", "Validate formula.")]
[TypeOption(typeof(CalculationEndpoints), "ValidateFormula")]
public class ValidateFormulaOption : CalculationEndpointBase<ValidateFormulaEndpoint>
{
}
