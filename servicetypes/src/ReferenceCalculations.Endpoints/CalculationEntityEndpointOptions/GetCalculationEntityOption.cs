using Fdw.Collections.Attributes;

namespace ReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The GetCalculationEntity endpoint.</summary>
[TypeOption(typeof(CalculationEntityEndpoints), "GetCalculationEntity")]
public class GetCalculationEntityOption : CalculationEntityEndpointBase<GetCalculationEntityEndpoint>
{
}
