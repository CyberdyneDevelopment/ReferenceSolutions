using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The CreatePlayer endpoint.</summary>
[WebMcpTool("create_player", "Create a new player")]
[TypeOption(typeof(NflEndpoints), "CreatePlayer")]
public class CreatePlayerOption : NflEndpointBase<CreatePlayerEndpoint>
{
}
