using Fdw.Collections.Attributes;

namespace ReferenceDdl.Endpoints.DdlEndpointOptions;

/// <summary>The GenerateDdl endpoint.</summary>
[TypeOption(typeof(DdlEndpoints), "GenerateDdl")]
public class GenerateDdlOption : DdlEndpointBase<GenerateDdlEndpoint>
{
}
