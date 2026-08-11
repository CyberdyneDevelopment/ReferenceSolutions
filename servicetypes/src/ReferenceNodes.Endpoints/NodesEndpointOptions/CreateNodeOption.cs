using Fdw.Collections.Attributes;

namespace ReferenceNodes.Endpoints.NodesEndpointOptions;

/// <summary>The CreateNode endpoint.</summary>
[TypeOption(typeof(NodesEndpoints), "CreateNode")]
public class CreateNodeOption : NodesEndpointBase<CreateNodeEndpoint>
{
}
