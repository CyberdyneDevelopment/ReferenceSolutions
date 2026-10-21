using Fdw.Collections.Attributes;

namespace FdwReferencePipelines.Endpoints.ScriptEndpointOptions;

/// <summary>The ValidateScript endpoint.</summary>
[TypeOption(typeof(ScriptEndpoints), "ValidateScript")]
public class ValidateScriptOption : ScriptEndpointBase<ValidateScriptEndpoint>
{
}
