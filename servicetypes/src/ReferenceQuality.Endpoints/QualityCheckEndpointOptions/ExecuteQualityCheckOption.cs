using Fdw.Collections.Attributes;

namespace ReferenceQuality.Endpoints.QualityCheckEndpointOptions;

/// <summary>The ExecuteQualityCheck endpoint.</summary>
[TypeOption(typeof(QualityCheckEndpoints), "ExecuteQualityCheck")]
public class ExecuteQualityCheckOption : QualityCheckEndpointBase<ExecuteQualityCheckEndpoint>
{
}
