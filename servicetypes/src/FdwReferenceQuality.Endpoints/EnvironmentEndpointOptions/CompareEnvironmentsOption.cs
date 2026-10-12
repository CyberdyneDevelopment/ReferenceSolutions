using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePromotion.Endpoints.EnvironmentEndpointOptions;

/// <summary>The CompareEnvironments endpoint.</summary>
[WebMcpTool("compare_environments", "Compare environments.")]
[TypeOption(typeof(EnvironmentEndpoints), "CompareEnvironments")]
public class CompareEnvironmentsOption : EnvironmentEndpointBase<CompareEnvironmentsEndpoint>
{
}
