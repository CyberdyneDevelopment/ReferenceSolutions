using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProjects.Endpoints.ProjectsEndpointOptions;

/// <summary>The DeleteProject endpoint.</summary>
[WebMcpTool("delete_project", "Delete an ETL project")]
[TypeOption(typeof(ProjectsEndpoints), "DeleteProject")]
public class DeleteProjectOption : ProjectsEndpointBase<DeleteProjectEndpoint>
{
}
