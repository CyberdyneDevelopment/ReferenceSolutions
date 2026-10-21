using Fdw.Collections.Attributes;

namespace FdwReferencePipelines.Endpoints.ScriptEndpointOptions;

/// <summary>The UpdateScript endpoint.</summary>
[TypeOption(typeof(ScriptEndpoints), "UpdateScript")]
public class UpdateScriptOption : ScriptEndpointBase<UpdateScriptEndpoint>
{
}
