using Fdw.Collections.Attributes;

namespace FdwReferencePipelines.Endpoints.ScriptEndpointOptions;

/// <summary>The GetScript endpoint.</summary>
[TypeOption(typeof(ScriptEndpoints), "GetScript")]
public class GetScriptOption : ScriptEndpointBase<GetScriptEndpoint>
{
}
