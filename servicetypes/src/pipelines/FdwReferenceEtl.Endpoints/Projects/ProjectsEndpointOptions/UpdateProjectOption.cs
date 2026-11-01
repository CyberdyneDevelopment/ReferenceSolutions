using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProjects.Endpoints.ProjectsEndpointOptions;

/// <summary>The UpdateProject endpoint.</summary>
[WebMcpTool("update_project", "Update an ETL project")]
[TypeOption(typeof(ProjectsEndpoints), "UpdateProject")]
public class UpdateProjectOption : ProjectsEndpointBase<UpdateProjectEndpoint>
{
}
