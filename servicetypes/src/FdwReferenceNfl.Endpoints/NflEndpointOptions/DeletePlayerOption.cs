using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The DeletePlayer endpoint.</summary>
[WebMcpTool("delete_player", "Delete a player")]
[TypeOption(typeof(NflEndpoints), "DeletePlayer")]
public class DeletePlayerOption : NflEndpointBase<DeletePlayerEndpoint>
{
}
