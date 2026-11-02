using Fdw.Collections.Attributes;

namespace FdwReferencePipelines.Endpoints.ScriptEndpointOptions;

/// <summary>The CreateScript endpoint.</summary>
[TypeOption(typeof(ScriptEndpoints), "CreateScript")]
public class CreateScriptOption : ScriptEndpointBase<CreateScriptEndpoint>
{
}
