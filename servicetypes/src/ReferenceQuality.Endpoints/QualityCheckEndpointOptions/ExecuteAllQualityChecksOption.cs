using Fdw.Collections.Attributes;

namespace ReferenceQuality.Endpoints.QualityCheckEndpointOptions;

/// <summary>The ExecuteAllQualityChecks endpoint.</summary>
[TypeOption(typeof(QualityCheckEndpoints), "ExecuteAllQualityChecks")]
public class ExecuteAllQualityChecksOption : QualityCheckEndpointBase<ExecuteAllQualityChecksEndpoint>
{
}
