using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The ListActingSteps endpoint (FDW-2282).</summary>
[WebMcpTool("list_acting_steps", "List the roles you have acted with, from your own session to the one in hand.")]
[TypeOption(typeof(AuthEndpoints), "ListActingSteps")]
public class ListActingStepsOption : AuthEndpointBase<ListActingStepsEndpoint>
{
}
