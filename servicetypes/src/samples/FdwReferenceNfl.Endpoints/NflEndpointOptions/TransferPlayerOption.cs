using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The TransferPlayer endpoint.</summary>
[WebMcpTool("transfer_player", "Transfer a player to a new team")]
[TypeOption(typeof(NflEndpoints), "TransferPlayer")]
public class TransferPlayerOption : NflEndpointBase<TransferPlayerEndpoint>
{
}
