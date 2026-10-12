using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProjects.Endpoints.ProjectsEndpointOptions;

/// <summary>The ListProjects endpoint.</summary>
[WebMcpTool("list_projects", "List all ETL projects", ReadOnly = true)]
[TypeOption(typeof(ProjectsEndpoints), "ListProjects")]
public class ListProjectsOption : ProjectsEndpointBase<ListProjectsEndpoint>
{
}
