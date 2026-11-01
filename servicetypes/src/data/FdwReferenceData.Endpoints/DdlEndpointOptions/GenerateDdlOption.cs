using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDdl.Endpoints.DdlEndpointOptions;

/// <summary>The GenerateDdl endpoint.</summary>
[WebMcpTool("generate_ddl", "Generate DDL from schema")]
[TypeOption(typeof(DdlEndpoints), "GenerateDdl")]
public class GenerateDdlOption : DdlEndpointBase<GenerateDdlEndpoint>
{
}
