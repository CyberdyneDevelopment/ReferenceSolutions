using Fdw.Collections.Attributes;

namespace ReferenceLineage.Endpoints.LineageEndpointOptions;

/// <summary>The GetFieldLineage endpoint.</summary>
[TypeOption(typeof(LineageEndpoints), "GetFieldLineage")]
public class GetFieldLineageOption : LineageEndpointBase<GetFieldLineageEndpoint>
{
}
