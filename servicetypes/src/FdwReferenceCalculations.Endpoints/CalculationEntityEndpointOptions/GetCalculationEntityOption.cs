using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The GetCalculationEntity endpoint.</summary>
[WebMcpTool("get_calculation_entity", "Get calculation entity.", ReadOnly = true)]
[TypeOption(typeof(CalculationEntityEndpoints), "GetCalculationEntity")]
public class GetCalculationEntityOption : CalculationEntityEndpointBase<GetCalculationEntityEndpoint>
{
}
