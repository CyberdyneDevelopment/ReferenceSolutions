using Fdw.Collections.Attributes;

namespace ReferenceLineage.Endpoints.LineageEndpointOptions;

/// <summary>The ExpandLineageNode endpoint.</summary>
[TypeOption(typeof(LineageEndpoints), "ExpandLineageNode")]
public class ExpandLineageNodeOption : LineageEndpointBase<ExpandLineageNodeEndpoint>
{
}
