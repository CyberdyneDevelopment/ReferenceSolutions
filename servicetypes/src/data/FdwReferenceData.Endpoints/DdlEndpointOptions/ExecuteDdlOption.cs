using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDdl.Endpoints.DdlEndpointOptions;

/// <summary>The ExecuteDdl endpoint.</summary>
[WebMcpTool("execute_ddl", "Execute DDL")]
[TypeOption(typeof(DdlEndpoints), "ExecuteDdl")]
public class ExecuteDdlOption : DdlEndpointBase<ExecuteDdlEndpoint>
{
}
