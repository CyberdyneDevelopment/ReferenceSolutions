using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The RetirePlayer endpoint.</summary>
[WebMcpTool("retire_player", "Retire a player")]
[TypeOption(typeof(NflEndpoints), "RetirePlayer")]
public class RetirePlayerOption : NflEndpointBase<RetirePlayerEndpoint>
{
}
