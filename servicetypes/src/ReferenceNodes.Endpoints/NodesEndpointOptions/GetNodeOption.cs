using Fdw.Collections.Attributes;

namespace ReferenceNodes.Endpoints.NodesEndpointOptions;

/// <summary>The GetNode endpoint.</summary>
[TypeOption(typeof(NodesEndpoints), "GetNode")]
public class GetNodeOption : NodesEndpointBase<GetNodeEndpoint>
{
}
