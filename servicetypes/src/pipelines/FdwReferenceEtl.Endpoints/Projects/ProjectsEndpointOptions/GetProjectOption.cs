using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProjects.Endpoints.ProjectsEndpointOptions;

/// <summary>The GetProject endpoint.</summary>
[WebMcpTool("get_project", "Get an ETL project by ID", ReadOnly = true)]
[TypeOption(typeof(ProjectsEndpoints), "GetProject")]
public class GetProjectOption : ProjectsEndpointBase<GetProjectEndpoint>
{
}
