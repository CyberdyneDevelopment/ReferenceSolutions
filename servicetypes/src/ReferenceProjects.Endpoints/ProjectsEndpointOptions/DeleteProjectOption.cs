using Fdw.Collections.Attributes;

namespace ReferenceProjects.Endpoints.ProjectsEndpointOptions;

/// <summary>The DeleteProject endpoint.</summary>
[TypeOption(typeof(ProjectsEndpoints), "DeleteProject")]
public class DeleteProjectOption : ProjectsEndpointBase<DeleteProjectEndpoint>
{
}
