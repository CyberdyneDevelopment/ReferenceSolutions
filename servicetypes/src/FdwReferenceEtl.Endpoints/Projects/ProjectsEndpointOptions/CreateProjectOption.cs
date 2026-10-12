using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProjects.Endpoints.ProjectsEndpointOptions;

/// <summary>The CreateProject endpoint.</summary>
[WebMcpTool("create_project", "Create an ETL project")]
[TypeOption(typeof(ProjectsEndpoints), "CreateProject")]
public class CreateProjectOption : ProjectsEndpointBase<CreateProjectEndpoint>
{
}
