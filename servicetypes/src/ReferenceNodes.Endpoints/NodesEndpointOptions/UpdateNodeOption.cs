using Fdw.Collections.Attributes;

namespace ReferenceNodes.Endpoints.NodesEndpointOptions;

/// <summary>The UpdateNode endpoint.</summary>
[TypeOption(typeof(NodesEndpoints), "UpdateNode")]
public class UpdateNodeOption : NodesEndpointBase<UpdateNodeEndpoint>
{
}
