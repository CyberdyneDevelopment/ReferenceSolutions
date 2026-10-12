using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The DeleteStat endpoint.</summary>
[WebMcpTool("delete_stat", "Delete a player game stat")]
[TypeOption(typeof(NflEndpoints), "DeleteStat")]
public class DeleteStatOption : NflEndpointBase<DeleteStatEndpoint>
{
}
