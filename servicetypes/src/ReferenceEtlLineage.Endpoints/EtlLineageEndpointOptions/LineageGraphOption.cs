using Fdw.Collections.Attributes;

namespace ReferenceEtlLineage.Endpoints.EtlLineageEndpointOptions;

/// <summary>The LineageGraph endpoint.</summary>
[TypeOption(typeof(EtlLineageEndpoints), "LineageGraph")]
public class LineageGraphOption : EtlLineageEndpointBase<LineageGraphEndpoint>
{
}
