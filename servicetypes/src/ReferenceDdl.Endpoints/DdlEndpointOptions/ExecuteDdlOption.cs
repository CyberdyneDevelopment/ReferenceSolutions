using Fdw.Collections.Attributes;

namespace ReferenceDdl.Endpoints.DdlEndpointOptions;

/// <summary>The ExecuteDdl endpoint.</summary>
[TypeOption(typeof(DdlEndpoints), "ExecuteDdl")]
public class ExecuteDdlOption : DdlEndpointBase<ExecuteDdlEndpoint>
{
}
