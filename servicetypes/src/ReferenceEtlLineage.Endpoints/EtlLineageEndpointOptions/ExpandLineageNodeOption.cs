using Fdw.Collections.Attributes;

namespace ReferenceEtlLineage.Endpoints.EtlLineageEndpointOptions;

/// <summary>The ExpandLineageNode endpoint.</summary>
[TypeOption(typeof(EtlLineageEndpoints), "ExpandLineageNode")]
public class ExpandLineageNodeOption : EtlLineageEndpointBase<ExpandLineageNodeEndpoint>
{
}
