using Fdw.Collections.Attributes;

namespace ReferenceProjects.Endpoints.ProjectsEndpointOptions;

/// <summary>The ListProjects endpoint.</summary>
[TypeOption(typeof(ProjectsEndpoints), "ListProjects")]
public class ListProjectsOption : ProjectsEndpointBase<ListProjectsEndpoint>
{
}
